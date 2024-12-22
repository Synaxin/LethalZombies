using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using LethalNetworkAPI;
using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;
using System.Linq;

namespace Zombies.Scripts
{
    public class ZombieNetworkManager : MonoBehaviour
    {
        System.Random rand = new System.Random();
        private LNetworkMessage<(ulong, ulong)> deadBodyMessage;
        private LNetworkMessage<ulong> bloodDropMessage;
        private LNetworkMessage<BodySyncInfo> bodySyncMessage;
        private LNetworkMessage<ulong> zombieWakeMessage;
        private LNetworkMessage<(NetworkObjectReference, ZombieSpawnInfo)> maskSpawnMessage;
        private LNetworkMessage<ZombieDeadInfo> zombieDeadMessage;
        private LNetworkMessage<ZombieSpawnInfo> maskSpawnResponseMessage;
        private LNetworkMessage<NetworkObjectReference> maskDespawnMessage;
        private LNetworkMessage<NetworkObjectReference> maskChangeBodyMessage;
        private Dictionary<NetworkObjectReference, List<ulong>> maskDespawnList = new Dictionary<NetworkObjectReference, List<ulong>>();
        private Dictionary<NetworkObjectReference, List<ulong>> maskSpawnList = new Dictionary<NetworkObjectReference, List<ulong>>();
        internal Dictionary<ulong, bool> instaSpawnList = new Dictionary<ulong, bool>();
        private LNetworkMessage<Dictionary<ulong, bool>> instaSpawnMessage;
        public static bool set = false;

        public ZombieNetworkManager()
        {
            if (!set)
            {
                Zombies.Logger.LogDebug("Setting Up Network Manager");
                deadBodyMessage = LNetworkMessage<(ulong, ulong)>.Connect("AddDeadBody");
                bloodDropMessage = LNetworkMessage<ulong>.Connect("BloodDrop");
                bodySyncMessage = LNetworkMessage<BodySyncInfo>.Connect("DeadBodySync");
                zombieWakeMessage = LNetworkMessage<ulong>.Connect("ZombieWake");
                maskSpawnMessage = LNetworkMessage<(NetworkObjectReference, ZombieSpawnInfo)>.Connect("MaskSpawn");
                zombieDeadMessage = LNetworkMessage<ZombieDeadInfo>.Connect("ZombieDead");
                maskDespawnMessage = LNetworkMessage<NetworkObjectReference>.Connect("DespawnMask");
                maskSpawnResponseMessage = LNetworkMessage<ZombieSpawnInfo>.Connect("MaskSpawnResponse");
                maskChangeBodyMessage = LNetworkMessage<NetworkObjectReference>.Connect("MaskBodyChange");
                instaSpawnMessage = LNetworkMessage<Dictionary<ulong, bool>>.Connect("InstaSpawnChange");


                deadBodyMessage.OnServerReceived += OnDeadMessageServer;

                bodySyncMessage.OnServerReceived += OnSyncMessageServer;
                bodySyncMessage.OnClientReceived += OnSyncMessageClient;

                bloodDropMessage.OnServerReceived += OnBloodDropServer;
                bloodDropMessage.OnClientReceived += OnBloodDropClient;

                zombieWakeMessage.OnServerReceived += OnZombieWakeServer;
                zombieWakeMessage.OnClientReceived += OnZombieWakeClient;

                maskSpawnMessage.OnServerReceived += OnMaskSpawnServer;
                maskSpawnMessage.OnClientReceived += OnMaskSpawnClient;

                zombieDeadMessage.OnServerReceived += OnZombieDeadServer;
                zombieDeadMessage.OnClientReceived += OnZombieDeadClient;

                maskDespawnMessage.OnServerReceived += OnDespawnServer;

                maskChangeBodyMessage.OnServerReceived += OnBodyChangeServer;
                maskChangeBodyMessage.OnClientReceived += OnBodyChangeClient;

                instaSpawnMessage.OnServerReceived += OnInstaSpawnChangeServer;
                instaSpawnMessage.OnClientReceived += OnInstaSpawnChangeClient;
                set = true;
            }
            
        }



        internal void SendDeadMessage(ulong id, ulong actualID)
        {
            Zombies.Logger.LogMessage($"Sent Dead Message from {id}");
            deadBodyMessage.SendServer((id, actualID));
        }

        private void OnDeadMessageServer((ulong, ulong) pID, ulong id2)
        {
            Zombies.Logger.LogMessage($"Received Dead Message from {pID.Item1}, {id2}");
            if (pID.Item2 == id2)
            {
                Zombies.Infection.AddDeadBody(pID.Item1);
            }
        }


        internal void SendSyncMessageServer(ulong id, PlayerControllerB body)
        {
            GameObject deadBody = body.deadBody.gameObject;
            if (deadBody == null)
            {
                return;
            }
            BodySyncInfo syncInfo = new BodySyncInfo(id, deadBody.transform.position);
            bodySyncMessage.SendServer(syncInfo);
        }
        internal void OnSyncMessageServer(BodySyncInfo info, ulong id)
        {
            if (!(StartOfRound.Instance.allPlayerScripts.Length >= (int)info.bodyID))
            {
                return;
            }
            PlayerControllerB serverBody = StartOfRound.Instance.allPlayerScripts[info.bodyID];
            if (serverBody == null)
            {
                return;
            }
            GameObject deadBody = serverBody.deadBody.gameObject;
            if (deadBody == null)
            {
                return;
            }
            BodySyncInfo serverInfo = new BodySyncInfo(info.bodyID, deadBody.transform.position);
            bodySyncMessage.SendClients(serverInfo);
        }

        internal void OnSyncMessageClient(BodySyncInfo info)
        {
            if (!(StartOfRound.Instance.allPlayerScripts.Length >= (int)info.bodyID))
            {
                return;
            }
            PlayerControllerB localBody = StartOfRound.Instance.allPlayerScripts[info.bodyID];
            if (localBody == null)
            {
                return;
            }
            DeadBodyInfo deadBody = localBody.deadBody;
            
            if (deadBody == null)
            {
                return;
            }
            GameObject localDeadBody = localBody.deadBody.gameObject;
            if (info.GetDifference(localDeadBody.transform.position, 2))
            {
                Zombies.Logger.LogDebug($"Perfoming Body Sync On {info.bodyID}");
                localDeadBody.transform.position = info.position;
            }
        }

        internal void SendBloodMessage(ulong playerID)
        {
            Zombies.Logger.LogDebug("SendBloodMessage");
            bloodDropMessage.SendServer(playerID);
        }

        private void OnBloodDropServer(ulong playerID, ulong id)
        {
            Zombies.Logger.LogDebug("OnBloodMessageServer");
            bloodDropMessage.SendClients(playerID);
        }

        private void OnBloodDropClient(ulong playerID) 
        {
            Zombies.Logger.LogDebug("OnBloodMessageClient");
            if (!(StartOfRound.Instance.allPlayerScripts.Length >= (int)playerID))
            {
                return;
            }
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerID];
            if (player == null)
            {
                return;
            }
            player.bloodDropTimer = -1;
            player.DropBlood(Vector3.down);
        }

        internal void SendWakeMessage(ulong playerID)
        {
            zombieWakeMessage.SendServer(playerID);
        }

        private void OnZombieWakeServer(ulong playerID, ulong id)
        {
            zombieWakeMessage.SendClients(playerID);
        }

        private void OnZombieWakeClient(ulong playerID)
        {
            if (!(StartOfRound.Instance.allPlayerScripts.Length >= (int)playerID))
            {
                return;
            }
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerID];
            if (player == null)
            {
                return;
            }
            player.bloodDropTimer = -1;
            player.AddBloodToBody();
            player.DropBlood(Vector3.down);
            player.DropBlood(new Vector3(0.7f, -0.0f, 0.7f));
            player.DropBlood(new Vector3(-0.7f, -0.0f, 0.7f));
            player.DropBlood(new Vector3(0.7f, -0.0f, -0.7f));
            player.DropBlood(new Vector3(-0.7f, -0.0f, -0.7f));
            if (player.deadBody != null)
            {
                for(int i = 0; i < player.deadBody.bodyParts.Length; i++)
                {
                    player.deadBody.bodyParts[i].drag = 0;
                    player.deadBody.bodyParts[i].AddForce(new Vector3(0, 5000, 0), ForceMode.Force);
                }
            }
        }

        internal void SendMaskSpawnMessage(PlayerControllerB player, ulong target)
        {
            if (!(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
            {
                return;
            }
            Zombies.Logger.LogMessage($"Sent Mask {player}");
            maskSpawnMessage.SendServer((new NetworkObjectReference(), new ZombieSpawnInfo(player.playerClientId, target)));
        }
        private void OnMaskSpawnServer((NetworkObjectReference, ZombieSpawnInfo) info, ulong playerID)
        {
            if (!(StartOfRound.Instance.allPlayerScripts.Length >= (int)playerID))
            {

                return;
            }
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[info.Item2.playerID];
            if (!(StartOfRound.Instance.allPlayerScripts.Length >= (int)playerID))
            {
                return;
            }
            PlayerControllerB player2 = StartOfRound.Instance.allPlayerScripts[info.Item2.targetID];
            if (player == null)
            {
                return;
            }
            if (player2 == null)
            {
                player2 = player;
            }
            if (player.deadBody == null)
            {
                return;
            }
            Vector3 position = player.deadBody.transform.position;
            Quaternion rotation = Quaternion.Euler(Vector3.zero);
            bool prox = false;
            if (info.Item2.playerID != info.Item2.targetID)
            {
                prox = true;
                Dictionary<ulong, Vector3> dict = Zombies.Infection.GetPositions();
                Vector3 pos = dict[info.Item2.targetID];
                Zombies.Logger.LogDebug($"OnSpawn {pos}");
                Zombies.Logger.LogDebug($"Target Player {info.Item2.targetID}, Pos1 {position}, pos2 {pos}");
                Vector3 direction = Vector3.Normalize(position - pos);
                direction.y = 0;
                rotation = Quaternion.LookRotation(direction);
                float lerpMod = Mathf.Clamp01(5 / Vector3.Distance(position, pos));
                position = Vector3.Lerp(position, pos, lerpMod);
            }
            NetworkObjectReference netObjectRef = RoundManager.Instance.SpawnEnemyGameObject(position, rotation.eulerAngles.y, -1, Zombies.maskEnemy);
            NetworkObject networkObject;
            if (netObjectRef.TryGet(out networkObject))
            {
                Zombies.Infection.AddZombie(netObjectRef, player);
                Debug.Log((object)"Got network object for mask enemy");
                MaskedPlayerEnemy component = networkObject.GetComponent<MaskedPlayerEnemy>();
                Zombies.AddZombie(component, prox);
                component.SetSuit(player.currentSuitID);
                component.mimickingPlayer = player;
                component.SetEnemyOutside(!player.isInsideFactory);
                component.SetVisibilityOfMaskedEnemy();
                //component.SetMaskType(5);
                //component.SetRunningServerRpc(true);
                
                player.redirectToEnemy = (EnemyAI)component;
                if ((UnityEngine.Object)player.deadBody != (UnityEngine.Object)null)
                    player.deadBody.DeactivateBody(false);
            }
            Zombies.Logger.LogMessage($"Received Mask Server {info.Item2}");
            maskSpawnMessage.SendClients((netObjectRef, new ZombieSpawnInfo(info.Item2.playerID, info.Item2.targetID, netObjectRef)));
            
        }

        private void OnMaskSpawnClient((NetworkObjectReference, ZombieSpawnInfo) info)
        {
            Zombies.Logger.LogMessage($"Received Client, {info.Item1}");
            StartOfRound.Instance.StartCoroutine(waitForMimicEnemySpawn(info.Item1, info.Item2, info.Item2.playerID));
        }


        private IEnumerator waitForMimicEnemySpawn(NetworkObjectReference netObjectRef, ZombieSpawnInfo info, ulong id)
        {
            Zombies.Logger.LogDebug("Entered Spawn Coroutine");
            NetworkObject netObject = (NetworkObject)null;
            if (!(StartOfRound.Instance.allPlayerScripts.Length >= (int)id))
            {
                yield break;
            }
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[id];
            if (player == null)
            {
                yield break;
            }
            float startTime = Time.realtimeSinceStartup;
            yield return (object)new WaitUntil((Func<bool>)(() => (double)Time.realtimeSinceStartup - (double)startTime > 20.0 || netObjectRef.TryGet(out netObject)));
            if ((UnityEngine.Object)player.deadBody == (UnityEngine.Object)null)
            {
                startTime = Time.realtimeSinceStartup;
                yield return (object)new WaitUntil((Func<bool>)(() => (double)Time.realtimeSinceStartup - (double)startTime > 20.0 || (UnityEngine.Object)player.deadBody != (UnityEngine.Object)null));
            }
            if (!((UnityEngine.Object)player.deadBody == (UnityEngine.Object)null))
            {
                
                if ((UnityEngine.Object)netObject != (UnityEngine.Object)null)
                {
                    Zombies.Logger.LogDebug("Spawn Coroutine Found Netobj");
                    player.deadBody.DeactivateBody(false);
                    MaskedPlayerEnemy component = netObject.GetComponent<MaskedPlayerEnemy>();
                        component.mimickingPlayer = player;
                        component.SetSuit(player.currentSuitID);
                        component.SetEnemyOutside(!player.isInsideFactory);
                    component.SetVisibilityOfMaskedEnemy();
                    component.creatureAnimator.SetTrigger("HitEnemy");
                    //component.SetMaskType(5);
                    player.redirectToEnemy = (EnemyAI)component;
                    //maskSpawnResponseMessage.SendServer(info);
                }
            }
        }

        internal void SendZombieDeadMessage(ZombieDeadInfo info)
        {
            Zombies.Logger.LogDebug("SendZombieDeadMessage");
            zombieDeadMessage.SendServer(info);
        }
                
        private void OnZombieDeadServer(ZombieDeadInfo info, ulong id)
        {
            Zombies.Logger.LogDebug("OnZombieDeadServer");
            if (!(StartOfRound.Instance.allPlayerScripts.Length >= (int)info.playerID))
            {
                return;
            }
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[info.playerID];
            if (player != null)
            {
                Zombies.Logger.LogDebug("OnZombieDeadServer2");
                NetworkObject netObj;
                if (info.enemy.TryGet(out netObj))
                {
                    MaskedPlayerEnemy component;
                    if (netObj.TryGetComponent<MaskedPlayerEnemy>(out component))
                    {
                        Zombies.RemoveZombie(component);
                    }
                    Transform[] parts = GetBodyPartArray(netObj.gameObject);
                    Vector3[] positions = new Vector3[parts.Length];
                    for (int i = 0; i < parts.Length; i++)
                    {
                        positions[i] = parts[i].position;
                        Zombies.Logger.LogDebug($"{parts[i].position}, {positions[i]}");
                    }
                    ZombieDeadInfo info2 = new ZombieDeadInfo(info.playerID, positions, info.enemy);
                    zombieDeadMessage.SendClients(info2);
                    //bodySpawnOwnerMessage.SendClient(info2, info2.playerID);
                }
            }

        }
        
        private void OnZombieDeadClient(ZombieDeadInfo info)
        {
            Zombies.Logger.LogDebug("OnZombieDeadClient");
            if (!(StartOfRound.Instance.allPlayerScripts.Length >= (int)info.playerID))
            {
                return;
            }
            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[info.playerID];
            if (player == null)
            {
                return;
            }
            NetworkObject netObj;
            if (info.enemy.TryGet(out netObj))
            {
                //Transform[] playerParts = new Transform[10];

                //player.deadBody.transform.position = info.position;

                Zombies.Logger.LogDebug("OnZombieDeadClient2");
                Zombies.Logger.LogDebug("OnZombieDeadClient3");
                Zombies.Logger.LogDebug($"{info.position},");
                Zombies.Logger.LogDebug("OnZombieDeadClient4");
                if (Zombies.BoundConfig.zombiesDropBodies.Value)
                {

                    player.deadBody.overrideSpawnPosition = true;
                    player.deadBody.gameObject.SetActive(true);
                    Zombies.SetReplacementModelVisible(player);
                    player.deadBody.SetBodyPartsKinematic(false);
                    for (int index = 0; index < info.position.Length; ++index)
                    {
                        Zombies.Logger.LogDebug($"OnZombieDeadClient5, {info.position[index]}");
                        player.deadBody.bodyParts[index].position = info.position[index];
                        player.deadBody.bodyParts[index].velocity = Vector3.zero;
                        //player.deadBody.bodyParts[index].angularVelocity = Vector3.zero;
                        //player.deadBody.bodyParts[index].ResetCenterOfMass();
                        //player.deadBody.bodyParts[index].ResetInertiaTensor();
                    }

                    player.deadBody.seenByLocalPlayer = false;
                    Zombies.Logger.LogDebug("OnZombieDeadClient6");

                    Zombies.Logger.LogDebug("OnZombieDeadClient7");
                    //player.deadBody.SetBodyPartsKinematic();
                    player.deadBody.deactivated = false;
                    Zombies.Logger.LogDebug("OnZombieDeadClient8");
                    player.redirectToEnemy = null;
                }
                
                Zombies.Logger.LogDebug("OnZombieDeadClient9");
                //StartCoroutine(waitForBodyStartFired(info));
                //StartCoroutine(waitForBodyInactive(player.deadBody, player.playerClientId));
                    if (netObj != null)
                    {
                        Zombies.Logger.LogDebug("OnZombieDeadClient10");
                    //netObj.Despawn();
                        if (Zombies.BoundConfig.droppedBodiesInfection.Value)
                        {
                            netObj.gameObject.SetActive(false);
                            maskDespawnMessage.SendServer(info.enemy);
                        }
                    }
                Zombies.Logger.LogDebug("OnZombieDeadClient11");
                if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
                {
                    if (Zombies.BoundConfig.droppedBodiesInfection.Value)
                    {
                        Zombies.Infection.AddDeadBody(player.playerClientId);
                    }
                }
            }
        }

        private void OnDespawnServer(NetworkObjectReference enemy, ulong id)
        {
            if (maskDespawnList.ContainsKey(enemy))
            {
                maskDespawnList[enemy].Add(id);
            }
            else
            {
                maskDespawnList.Add(enemy, new List<ulong>());
                maskDespawnList[enemy].Add(id);
            }
            if (maskDespawnList[enemy].Count >= StartOfRound.Instance.ClientPlayerList.Count)
            {
                NetworkObject netObj;
                if (enemy.TryGet(out netObj))
                {
                    netObj.Despawn();
                }
                maskDespawnList.Remove(enemy);
            }
        }

        internal void SendBodyChangeMessage(NetworkObjectReference reference)
        {
            maskChangeBodyMessage.SendServer(reference);
        }

        private void OnBodyChangeServer(NetworkObjectReference reference, ulong id)
        {
            maskChangeBodyMessage.SendClients(reference);
        }

        private void OnBodyChangeClient(NetworkObjectReference reference)
        {
            StartOfRound.Instance.StartCoroutine(waitForBodyChangeRef(reference));
        }

        private IEnumerator waitForBodyChangeRef(NetworkObjectReference reference)
        {
            Zombies.Logger.LogDebug("Entered Spawn Coroutine");
            NetworkObject netObject1 = (NetworkObject)null;
            Zombies.Logger.LogMessage("Changing Body");
            float startTime = Time.realtimeSinceStartup;
            yield return (object)new WaitUntil((Func<bool>)(() => (double)Time.realtimeSinceStartup - (double)startTime > 10.0 || reference.TryGet(out netObject1)));
            if ((UnityEngine.Object)netObject1 != (UnityEngine.Object)null)
            {
                Zombies.Logger.LogDebug("Change Body Coroutine Found Netobj");
                MaskedPlayerEnemy component1 = netObject1.GetComponent<MaskedPlayerEnemy>();
                if (component1.mimickingPlayer != null)
                {
                    component1.mimickingPlayer.redirectToEnemy = null;
                }
                
                    
            }
        }

        internal void SendInstaSpawnChange(Dictionary<ulong, bool> dict)
        {
            instaSpawnMessage.SendServer(dict);
        }

        private void OnInstaSpawnChangeServer(Dictionary<ulong, bool> dict, ulong id)
        {
            instaSpawnMessage.SendClients(dict);
        }

        private void OnInstaSpawnChangeClient(Dictionary<ulong, bool> dict)
        {
            instaSpawnList = dict;
        }

        private Transform[] GetBodyPartArray(GameObject mask)
        {
            Transform[] partArray = new Transform[11];
            GameObject rig = mask.transform.Find("ScavengerModel").Find("metarig").gameObject;
            Transform spine = rig.transform.Find("spine");
            Transform spine002 = spine.GetChild(0).GetChild(0);
            Transform spine003 = spine002.GetChild(0);
            Transform spine004 = spine003.Find("spine.004");
            Transform l_arm = spine003.Find("shoulder.L");
            Transform l_arm_upper = l_arm.GetChild(0);
            Transform l_arm_lower = l_arm_upper.GetChild(0);
            Transform r_arm = spine003.Find("shoulder.R");
            Transform r_arm_upper = r_arm.GetChild(0);
            Transform r_arm_lower = r_arm_upper.GetChild(0);
            Transform l_thigh = spine.Find("thigh.L");
            Transform l_shin = l_thigh.GetChild(0);
            Transform r_thigh = spine.Find("thigh.R");
            Transform r_shin = r_thigh.GetChild(0);

            partArray[0] = spine004;
            partArray[1] = r_arm_lower;
            partArray[2] = l_arm_lower;
            partArray[3] = r_shin;
            partArray[4] = l_shin;
            partArray[5] = spine002;
            partArray[6] = mask.transform;
            partArray[7] = r_thigh;
            partArray[8] = l_thigh;
            partArray[9] = l_arm_upper;
            partArray[10] = r_arm_upper;
            
            return partArray;

            //Zombies.Logger.LogMessage(spine.gameObject.name);

        }
    }



}
