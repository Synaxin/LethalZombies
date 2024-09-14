using System;
using System.Collections.Generic;
using System.Text;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using LethalNetworkAPI;

namespace Zombies.Scripts
{
    internal class InfectionHandler
    {

        //LethalServerMessage<int> deadBodyMessage = new LethalServerMessage<int>("AddDeadBody");
        //One tick ~= 5s
        private static int deadTicks = Mathf.Abs(Zombies.BoundConfig.tryForTicks.Value);
        private static float infectionChance = Mathf.Abs(Zombies.BoundConfig.infectionChance.Value); //0.8f;
        private static int infectionMinTicks = Mathf.Abs(Zombies.BoundConfig.infectionTimeMin.Value); //12
        private static int infectionMaxTicks = Mathf.Abs(Zombies.BoundConfig.infectionTimeMax.Value); //36;
        private static int proxChance = Mathf.Abs(Zombies.BoundConfig.proximityChance.Value); //25;

        private static float timeForTick = 2; //5;
        private static float currentTickTime = 0;

        private static float timeForProxTick = 0.1f;
        private static float currentProxTickTime = 0;
        private static bool tickProximity = false;
        private static float proxDistance = 5;

        private static int wakeTicks = 10;
        private static float timeForWakeTick = 0.2f;
        private static float currentWakeTickTime = 0f;
        private static bool tickWake = false;
        private static Dictionary<PlayerControllerB, InfectionInfo> deadList = new Dictionary<PlayerControllerB, InfectionInfo>();
        private static Dictionary<NetworkObjectReference, PlayerControllerB> bodyList = new Dictionary<NetworkObjectReference, PlayerControllerB>();


        public void ClearInfected()
        {
            
            deadList.Clear();
            bodyList.Clear();
        }

        public void AddDeadBody(ulong playerID)
        {
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player.playerClientId == playerID)
                {
                    Zombies.Logger.LogMessage($"Adding Player {player.playerClientId} to deadlist");
                    bool good = true;
                    foreach(var (netRef, playerControl) in bodyList)
                    {
                        if (player == playerControl)
                        {
                            good = false;
                            Zombies.Logger.LogMessage("Cancelling Add");
                        }
                    }
                    if (good) //Don't add if player already in zombie list, which means they were converted by a mask
                    {
                        deadList.Add(player, new InfectionInfo(player, deadTicks, infectionChance, infectionMinTicks, infectionMaxTicks, proxChance, wakeTicks));
                    }
                }
            }
            
        }

        public void AddZombie(NetworkObjectReference enemy, PlayerControllerB player)
        {
            if (player == null || bodyList.ContainsKey(enemy))
            {
                return;
            }
            Zombies.Logger.LogDebug("Added Player to Zombie List");
            if (deadList.ContainsKey(player))
            {
                deadList.Remove(player);
            }
            bodyList.Add(enemy, player);
        }

        public void ReplaceDeadBody(NetworkObject target)
        {
            List<NetworkObjectReference> removeRef = new List<NetworkObjectReference>();
            foreach (var (reference, body) in bodyList)
            {
                NetworkObject netObj;
                if (reference.TryGet(out netObj))
                {
                    if (netObj == target)
                    {
                        Vector3[] list = new Vector3[1];
                        list[0] = target.transform.position;
                        Zombies.Networking.SendZombieDeadMessage(new ZombieDeadInfo(body.playerClientId, list, reference));
                        removeRef.Add(reference);
                    }
                }
            }
            foreach(var reference in removeRef)
            {
                if (bodyList.ContainsKey(reference))
                {
                    Zombies.Logger.LogDebug("Removing Reference");
                    PlayerControllerB player = bodyList[reference];
                    bodyList.Remove(reference);
                    AddDeadBody(player.playerClientId);
                }
            }
        }
        

        public void TickThroughBodies()
        {
            if (!(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
            {
                return;
            }
            if (deadList.Count > 0)
            {
                TickInfection();
                if (tickProximity) //Found Proximity
                {
                    
                    ProximityCheck();
                }
                if (tickWake) //Found Wake
                {
                    
                    WakeTick();
                }
                
            }
            else
            {
                currentTickTime = 0;
            }
        }

        private void TickInfection()
        {
            bool foundProx = false;
            bool foundWake = false;
            currentTickTime += Time.deltaTime;
            if (currentTickTime >= timeForTick)
            {
                List<PlayerControllerB> removalList = new List<PlayerControllerB>();
                currentTickTime = 0;
                foreach (var (body, info) in deadList)
                {
                    if (info.GetState() < 0 || body.deadBody == null || body.deadBody.deactivated)
                    {
                        removalList.Add(body);
                        continue;
                    }
                    Zombies.Networking.SendSyncMessageServer(body.playerClientId, body);
                    int state = info.Tick();
                    
                    Zombies.Logger.LogDebug($"Body {body} Ticked! State: {state}");
                    /*
                    Zombies.Logger.LogMessage(state == 1);
                    if (state == 1)
                    {
                        Zombies.Logger.LogMessage("Infected!");
                        //body.ResetPlayerBloodObjects();
                    }
                    else*/
                    if (state == 2)
                    {
                        //body.bloodDropTimer = -1;
                        //RPCStuff.BloodServerRpc(body);
                        //RPCStuff.BroadCastBlood(body);
                        Zombies.Networking.SendBloodMessage(body.playerClientId);
                        Zombies.Logger.LogDebug($"Ticking Infection! ID{body.playerClientId}");
                    }
                    else if (state == 3)
                    {
                        Zombies.Logger.LogDebug($"Infection Complete! ID{body.playerClientId}");
                        info.SetTarget(body);
                        info.WakeUp();
                    }
                    else if (state == 4)
                    {
                        Zombies.Logger.LogDebug("Infection Complete! Proximity ID{body.playerClientId}");
                        foundProx = true;
                    }
                    else if (state == 5)
                    {
                        foundWake = true;
                    }
                }
                foreach (var body in removalList)
                {
                    deadList[body].Finish();
                    deadList.Remove(body);
                }
                tickProximity = foundProx;
                tickWake = foundWake;
            }
            
        }
        private void ProximityCheck()
        {
            currentProxTickTime += Time.deltaTime;
            if (currentProxTickTime >= timeForProxTick)
            {
                Zombies.Logger.LogDebug("Ticking Proximity!");
                currentProxTickTime = 0;
                foreach (var (body, info) in deadList)
                {
                    if (info.GetState() == 4)
                    {
                        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
                        {
                            if (player.isPlayerControlled && !player.isPlayerDead && player.gameObject != body.gameObject)
                            {
                                if (body.deadBody != null)
                                {
                                    float distance = Vector3.Distance(body.deadBody.gameObject.transform.position, player.gameObject.transform.position);
                                    if (distance <= proxDistance)
                                    {
                                        Zombies.Logger.LogDebug("Found Target!");
                                        info.SetTarget(player);
                                        info.WakeUp();
                                        tickWake = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void WakeTick()
        {
            currentWakeTickTime += Time.deltaTime;
            if (currentWakeTickTime >= timeForWakeTick)
            {
                Zombies.Logger.LogDebug("Ticking Wake!");
                currentWakeTickTime = 0;
                List<PlayerControllerB> removalList = new List<PlayerControllerB>();
                foreach (var (body, info) in deadList)
                {
                    if (info.GetState() == 5)
                    {
                        info.WakeTick();
                        Zombies.Networking.SendWakeMessage(body.playerClientId);
                    }
                    if (info.GetState() == 6)
                    {
                        removalList.Add(body);
                    }
                }
                foreach(PlayerControllerB body in removalList)
                {
                    deadList[body].Finish();
                    Zombies.Networking.SendMaskSpawnMessage(body, deadList[body].GetTarget().playerClientId);
                    deadList.Remove(body);
                }
            }
        }


        public Dictionary<ulong, Vector3> GetPositions()
        {
            Dictionary<ulong, Vector3> dict = new Dictionary<ulong, Vector3>();
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player.isPlayerControlled)
                {
                    dict.Add(player.playerClientId, player.transform.position);
                }
            }
            return dict;
        }
    }
}
