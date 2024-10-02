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

        private static int wakeTicks = Zombies.BoundConfig.wakeTickAmount.Value; //10
        private static int proxWakeTicks = Zombies.BoundConfig.proxWakeTickAmount.Value;
        private static int proxHeldWakeTicks = Zombies.BoundConfig.proxHeldWakeTickAmount.Value;
        private static float timeForWakeTick = 0.2f;
        private static float currentWakeTickTime = 0f;
        private static bool tickWake = false;

        private static bool reviveOnDeath = Zombies.BoundConfig.reviveOnDeath.Value;
        private static float reviveOnDeathChance = Zombies.BoundConfig.reviveOnDeathChance.Value;
        private static bool reviveOnDeathAlone = Zombies.BoundConfig.onlyReviveWhileAlone.Value;
        
        private static Dictionary<PlayerControllerB, InfectionInfo> deadList = new Dictionary<PlayerControllerB, InfectionInfo>();
        private static Dictionary<NetworkObjectReference, PlayerControllerB> bodyList = new Dictionary<NetworkObjectReference, PlayerControllerB>();
        private static Dictionary<PlayerControllerB, InfectionInfo> infectedList = new Dictionary<PlayerControllerB, InfectionInfo>();
        private static List<PlayerControllerB> deadConvertedList = new List<PlayerControllerB>();
        private static List<PlayerControllerB> instaExceptionsList = new List<PlayerControllerB>();

        private int previousLiving = 0;

        private System.Random rand = new System.Random();

        public void Reset()
        {
            deadList.Clear();
            bodyList.Clear();
            infectedList.Clear();
            deadConvertedList.Clear();
            tickProximity = false;
            tickWake = false;
            currentTickTime = 0;
            currentWakeTickTime = 0;
            currentProxTickTime = 0;
        }

        public bool GetReviveAlone()
        {
            return reviveOnDeathAlone;
        }

        public void RollInstaSpawn()
        {
            Dictionary<ulong, bool> dict = new Dictionary<ulong, bool>();
            foreach(PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player.isPlayerControlled)
                {
                    bool insta = rand.Next(0, 100) <= reviveOnDeathChance;
                    dict.Add(player.actualClientId, insta);
                }
            }
            Zombies.Networking.instaSpawnList.Clear();
            Zombies.Networking.SendInstaSpawnChange(dict);
        }

        public void AppendInstaSpawn(Dictionary<ulong, bool> dict)
        {
            Dictionary<ulong, bool> newDict = Zombies.Networking.instaSpawnList;
            foreach (var (id, insta) in dict)
            {
                if (newDict.ContainsKey(id))
                {
                    newDict[id] = insta;
                }
                else
                {
                    newDict.Add(id, insta);
                }
            }
            Zombies.Networking.SendInstaSpawnChange(newDict);
        }

        public bool RollInstaSpawn(ulong playerID)
        {
            bool insta = rand.Next(0, 100) <= reviveOnDeathChance;
            return insta;
        }

        public void ClearInfected()
        {
            
            deadList.Clear();
            bodyList.Clear();
            deadConvertedList.Clear();
            instaExceptionsList.Clear();
        }

        public void AddDeadBody(ulong playerID)
        {
            if (StartOfRound.Instance.allPlayerScripts.Length >= (int)playerID)
            {
                PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[(int)playerID];
                if (!deadConvertedList.Contains(player))
                {
                    bool good1 = false;
                    if (reviveOnDeath && StartOfRound.Instance.shipHasLanded && ! instaExceptionsList.Contains(player))
                    {
                        Zombies.Logger.LogDebug("Check for Zombie -- Killplayer");
                        if (Zombies.Networking.instaSpawnList.ContainsKey(player.actualClientId))
                        {
                            Zombies.Logger.LogDebug("Zombie In Spawnlist -- Killplayer");
                        
                            if (Zombies.Networking.instaSpawnList[player.actualClientId])
                            {
                                Zombies.Logger.LogDebug("Insta Spawn on -- Killplayer");
                                good1 = true;
                                if (Zombies.Infection.GetReviveAlone() && !player.isPlayerAlone)
                                {
                                    Zombies.Logger.LogDebug("Not Alone -- Killplayer");
                                    good1 = false;
                                }
                            }
                            if (good1)
                            {
                                Zombies.Logger.LogDebug("Spawning Zombie -- Killplayer");
                                if (Zombies.Infection != null)
                                {
                                    if (!instaExceptionsList.Contains(player))
                                    {
                                        instaExceptionsList.Add(player);
                                    }
                                    Zombies.Networking.SendMaskSpawnMessage(player, player.playerClientId);
                                    return;
                                }
                            }

                        }
                    }
                    if (!good1)
                    {
                        Zombies.Logger.LogMessage($"Adding Player {player.playerClientId} to deadlist");
                        bool good = true;
                        foreach (var (netRef, playerControl) in bodyList)
                        {
                            if (player == playerControl)
                            {
                                good = false;
                                Zombies.Logger.LogMessage("Cancelling Add");
                            }
                        }
                        if (good) //Don't add if player already in zombie list, which means they were converted by a mask
                        {
                            if (!deadList.ContainsKey(player))
                            {
                                if (!instaExceptionsList.Contains(player))
                                {
                                    instaExceptionsList.Add(player);
                                }
                                deadList.Add(player, new InfectionInfo(player, deadTicks, infectionChance, infectionMinTicks, infectionMaxTicks, proxChance, wakeTicks));
                                if (deadList.ContainsKey(player))
                                {
                                    if (deadList[player].GetProximity())
                                    {
                                        deadList[player].SetWakeTicks(proxWakeTicks);
                                    }
                                }

                            }
                        }
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
            if (bodyList.ContainsValue(player))
            {
                List<NetworkObjectReference> oldEnemy = new List<NetworkObjectReference>();
                bool set = false;
                foreach (var (reference, body) in bodyList)
                {
                    if (body == player)
                    {
                        oldEnemy.Add(reference);
                        set = true;
                    }
                }
                if (set)
                {
                    foreach(NetworkObjectReference val in oldEnemy)
                    {
                        Zombies.Networking.SendBodyChangeMessage(val);
                        bodyList.Remove(val);
                    }
                }
            }
            bodyList.Add(enemy, player);
            if (!Zombies.BoundConfig.droppedBodiesInfection.Value || !Zombies.BoundConfig.zombiesDropBodies.Value)
            {
                deadConvertedList.Add(player);
            }
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
        
        public void TickThroughRevivals()
        {
            if (previousLiving < StartOfRound.Instance.livingPlayers && StartOfRound.Instance.shipHasLanded)
            {
                if (bodyList.Count > 0)
                {
                    List<NetworkObjectReference> removalList = new List<NetworkObjectReference>();
                    foreach(var (reference, body) in bodyList)
                    {
                        if (!body.isPlayerDead)
                        {
                            Zombies.Logger.LogDebug("Revived player removed from bodylist");
                            removalList.Add(reference);
                            if (reviveOnDeath)
                            {
                                if (Zombies.Networking.instaSpawnList.ContainsKey(body.actualClientId))
                                {
                                    Zombies.Networking.instaSpawnList[body.actualClientId] = RollInstaSpawn(body.actualClientId);
                                }
                                else
                                {
                                    Zombies.Networking.instaSpawnList.Add(body.actualClientId, RollInstaSpawn(body.actualClientId));
                                }
                            }
                            
                        }
                    }
                    foreach (NetworkObjectReference val in removalList)
                    {
                        
                        Zombies.Networking.SendBodyChangeMessage(val);
                        bodyList.Remove(val);
                    }
                }
            }
            previousLiving = StartOfRound.Instance.livingPlayers;
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
                    try
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
                            if (!info.GetProximity() || (info.GetProximity() && info.GetState() == 4 && !info.GetParentSet()))
                            {
                                Zombies.Networking.SendBloodMessage(body.playerClientId);
                            }
                            Zombies.Logger.LogDebug($"Ticking Infection! ID{body.actualClientId}");
                        }
                        else if (state == 3)
                        {
                            Zombies.Logger.LogDebug($"Infection Complete! ID{body.actualClientId}");
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
                    catch
                    {
                        Zombies.Logger.LogWarning("Tried to tick infection on a missing player! This is probably because a player left.");
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
                int proxBodies = 0;
                Zombies.Logger.LogDebug("Ticking Proximity!");
                currentProxTickTime = 0;
                foreach (var (body, info) in deadList)
                {
                    if (info.GetState() == 4)
                    {
                        proxBodies++;
                        foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
                        {
                            if (player.isPlayerControlled && !player.isPlayerDead && player.gameObject != body.gameObject)
                            {
                                if (body.deadBody != null)
                                {
                                    float distance = Vector3.Distance(body.deadBody.gameObject.transform.position, player.gameObject.transform.position);
                                    if (distance <= proxDistance)
                                    {
                                        if (!info.GetParentSet())
                                        {
                                            bool found = false;
                                            if (player.currentlyHeldObjectServer != null)
                                            {
                                                Zombies.Logger.LogDebug($"Held Object = {player.currentlyHeldObjectServer.name}");
                                                if (player.currentlyHeldObjectServer.gameObject.name == "RagdollGrabbableObject(Clone)")
                                                {
                                                    Zombies.Logger.LogDebug($"After Held Object");
                                                    if (player.currentlyHeldObjectServer.transform.parent.parent.parent.gameObject == body.deadBody.gameObject)
                                                    {
                                                        found = true;
                                                        Zombies.Logger.LogDebug("Zombie Adopted!");
                                                        info.SetParentalFigure(player, player.currentlyHeldObjectServer.gameObject);
                                                        info.SetWakeTicks(proxHeldWakeTicks);
                                                    }
                                                }
                                            }
                                            
                                            if (!found)
                                            {
                                                Zombies.Logger.LogDebug("Found Target! No parent");
                                                info.SetTarget(player);
                                                info.WakeUp();
                                                proxBodies--;
                                                tickWake = true;
                                            }
                                        }
                                        else
                                        {
                                            if (player != info.GetParentalFigure())
                                            {
                                                Zombies.Logger.LogDebug("Found Target!");
                                                info.SetTarget(player);
                                                info.WakeUp();
                                                proxBodies--;
                                                tickWake = true;
                                            }
                                            else
                                            {
                                                bool found = false;
                                                if (player.currentlyHeldObjectServer != null)
                                                {
                                                    if (player.currentlyHeldObjectServer.gameObject == info.GetRagdoll())
                                                    {
                                                        found = true;
                                                    }
                                                }
                                                if (!found)
                                                {
                                                    Zombies.Logger.LogDebug("Found Target! !found");
                                                    info.SetTarget(player);
                                                    info.WakeUp();
                                                    proxBodies--;
                                                    tickWake = true;
                                                }
                                            }
                                        }
                                        
                                    }
                                }
                            }
                        }
                    }
                }
                if (proxBodies <= 0)
                {
                    tickProximity = false;
                }
            }
        }

        private void WakeTick()
        {
            currentWakeTickTime += Time.deltaTime;
            if (currentWakeTickTime >= timeForWakeTick)
            {
                int wakingBodies = 0;
                Zombies.Logger.LogDebug("Ticking Wake!");
                currentWakeTickTime = 0;
                List<PlayerControllerB> removalList = new List<PlayerControllerB>();
                foreach (var (body, info) in deadList)
                {
                    if (info.GetState() == 5)
                    {
                        wakingBodies++;
                        Zombies.Logger.LogDebug($"Wake Tick on {body.actualClientId}");
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
                    Zombies.Networking.SendMaskSpawnMessage(body, deadList[body].GetTarget().actualClientId);
                    deadList.Remove(body);
                }
                if (wakingBodies <= 0)
                {
                    tickWake = false;
                }
                Zombies.Logger.LogDebug("Wake Tick Complete!");
            }
        }


        public Dictionary<ulong, Vector3> GetPositions()
        {
            Dictionary<ulong, Vector3> dict = new Dictionary<ulong, Vector3>();
            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts)
            {
                if (player.isPlayerControlled)
                {
                    dict.Add(player.actualClientId, player.transform.position);
                }
            }
            return dict;
        }
    }
}
