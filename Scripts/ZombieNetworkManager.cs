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
        private readonly LNetworkMessage<ulong> deadBodyMessage;
        private readonly LNetworkMessage<ulong> bloodDropMessage;
        private readonly LNetworkMessage<BodySyncInfo> bodySyncMessage;
        private readonly LNetworkMessage<ulong> zombieWakeMessage;
        private readonly LNetworkMessage<(NetworkObjectReference, ZombieSpawnInfo)> maskSpawnMessage;

        public ZombieNetworkManager()
        {
            deadBodyMessage = LNetworkMessage<ulong>.Connect("AddDeadBody");
            bloodDropMessage = LNetworkMessage<ulong>.Connect("BloodDrop");
            bodySyncMessage = LNetworkMessage<BodySyncInfo>.Connect("DeadBodySync");
            zombieWakeMessage = LNetworkMessage<ulong>.Connect("ZombieWake");
            maskSpawnMessage = LNetworkMessage<(NetworkObjectReference, ZombieSpawnInfo)>.Connect("MaskSpawn");


            deadBodyMessage.OnServerReceived += OnDeadMessageServer;
            bodySyncMessage.OnServerReceived += OnSyncMessageServer;
            bodySyncMessage.OnClientReceived += OnSyncMessageClient;
            bloodDropMessage.OnServerReceived += OnBloodDropServer;
            bloodDropMessage.OnClientReceived += OnBloodDropClient;
            zombieWakeMessage.OnServerReceived += OnZombieWakeServer;
            zombieWakeMessage.OnClientReceived += OnZombieWakeClient;
            maskSpawnMessage.OnServerReceived += OnMaskSpawnServer;
            maskSpawnMessage.OnClientReceived += OnMaskSpawnClient;
        }

        internal void SendDeadMessage(ulong id)
        {
            Zombies.Logger.LogMessage($"Sent Dead Message from {id}");
            deadBodyMessage.SendServer(id);
        }

        private void OnDeadMessageServer(ulong id, ulong id2)
        {
            Zombies.Logger.LogMessage($"Received Dead Message from {id}, {id2}");
            if (id == id2)
            {
                Zombies.Infection.AddDeadBody(id);
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
            PlayerControllerB serverBody = info.bodyID.GetPlayerController();
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
            PlayerControllerB localBody = info.bodyID.GetPlayerController();
            if (localBody == null)
            {
                return;
            }
            GameObject localDeadBody = localBody.deadBody.gameObject;
            if (localDeadBody == null)
            {
                return;
            }
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
            PlayerControllerB player = playerID.GetPlayerController();
            if (player == null)
            {
                return;
            }
            player.bloodDropTimer = -1;
            player.DropBlood(Vector3.down);
        }

        internal void SendWakeMessage(ulong playerID)
        {
            Zombies.Logger.LogDebug("SendWakeMessage");
            zombieWakeMessage.SendServer(playerID);
        }

        private void OnZombieWakeServer(ulong playerID, ulong id)
        {
            Zombies.Logger.LogDebug("OnZombieWakeServer");
            zombieWakeMessage.SendClients(playerID);
        }

        private void OnZombieWakeClient(ulong playerID)
        {
            Zombies.Logger.LogDebug("OnZombieWakeClient");
            PlayerControllerB player = playerID.GetPlayerController();
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
            PlayerControllerB player = info.Item2.playerID.GetPlayerController();
            if (player == null)
            {
                return;
            }
            if (player.deadBody == null)
            {
                return;
            }
            Vector3 position = player.deadBody.transform.position;
            if (info.Item2.playerID != info.Item2.targetID)
            {
                Dictionary<ulong, Vector3> dict = Zombies.Infection.GetPositions();
                Vector3 pos = dict[info.Item2.targetID];
                Zombies.Logger.LogDebug($"OnSpawn {pos}");
                Zombies.Logger.LogDebug($"Target Player {info.Item2.targetID}, Pos1 {position}, pos2 {pos}");
                float lerpMod = Mathf.Clamp01(5 / Vector3.Distance(position, pos));
                position = Vector3.Lerp(position, pos, lerpMod);
            }
            NetworkObjectReference netObjectRef = RoundManager.Instance.SpawnEnemyGameObject(position, 0, -1, Zombies.maskEnemy);
            NetworkObject networkObject;
            if (netObjectRef.TryGet(out networkObject))
            {
                Debug.Log((object)"Got network object for mask enemy");
                MaskedPlayerEnemy component = networkObject.GetComponent<MaskedPlayerEnemy>();
                component.SetSuit(player.currentSuitID);
                component.mimickingPlayer = player;
                component.SetEnemyOutside(!player.isInsideFactory);
                component.SetVisibilityOfMaskedEnemy();
                component.SetMaskType(5);
                player.redirectToEnemy = (EnemyAI)component;
                if ((UnityEngine.Object)player.deadBody != (UnityEngine.Object)null)
                    player.deadBody.DeactivateBody(false);
            }
            Zombies.Logger.LogMessage($"Received Mask Server {info.Item2}");
            maskSpawnMessage.SendClients((netObjectRef, new ZombieSpawnInfo(info.Item2.playerID, 0)));
            
        }
        private void OnMaskSpawnClient((NetworkObjectReference, ZombieSpawnInfo) info)
        {
            Zombies.Logger.LogMessage($"Received Client, {info.Item1}");
            this.StartCoroutine(this.waitForMimicEnemySpawn(info.Item1, info.Item2.playerID));
        }


        private IEnumerator waitForMimicEnemySpawn(NetworkObjectReference netObjectRef, ulong id)
        {
            NetworkObject netObject = (NetworkObject)null;
            PlayerControllerB player = id.GetPlayerController();
            if (player == null)
            {
                yield break;
            }
            Zombies.Logger.LogMessage(id.GetPlayerController());
            float startTime = Time.realtimeSinceStartup;
            yield return (object)new WaitUntil((Func<bool>)(() => (double)Time.realtimeSinceStartup - (double)startTime > 20.0 || netObjectRef.TryGet(out netObject)));
            if ((UnityEngine.Object)id.GetPlayerController().deadBody == (UnityEngine.Object)null)
            {
                startTime = Time.realtimeSinceStartup;
                yield return (object)new WaitUntil((Func<bool>)(() => (double)Time.realtimeSinceStartup - (double)startTime > 20.0 || (UnityEngine.Object)id.GetPlayerController().deadBody != (UnityEngine.Object)null));
            }
            if (!((UnityEngine.Object)id.GetPlayerController().deadBody == (UnityEngine.Object)null))
            {
                
                if ((UnityEngine.Object)netObject != (UnityEngine.Object)null)
                {
                    id.GetPlayerController().deadBody.DeactivateBody(false);
                    MaskedPlayerEnemy component = netObject.GetComponent<MaskedPlayerEnemy>();
                    component.mimickingPlayer = id.GetPlayerController();
                    component.SetSuit(id.GetPlayerController().currentSuitID);
                    component.SetEnemyOutside(!id.GetPlayerController().isInsideFactory);
                    component.SetVisibilityOfMaskedEnemy();
                    component.SetMaskType(5);
                    id.GetPlayerController().redirectToEnemy = (EnemyAI)component;
                }
            }
        }
    }


}
