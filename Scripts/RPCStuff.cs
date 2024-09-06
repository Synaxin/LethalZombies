using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;
using LethalNetworkAPI;

namespace Zombies.Scripts
{
    class RPCStuff
    {
        /*
        [ServerRpc(RequireOwnership = false)]
        public static void PlayerDiedServerRpc(int playerClientID)
        {
            Zombies.Logger.LogMessage($"Player {playerClientID} died! Logging death.");
            InfectionHandler.AddDeadBody(playerClientID);
        }

        [ServerRpc(RequireOwnership = false)]
        public static void BloodServerRpc(PlayerControllerB player)
        {
            Zombies.Logger.LogMessage("Blood Dropped Server!");
            BroadCastBloodClientRpc(player);
        }

        [ClientRpc]
        public static void BroadCastBloodClientRpc(PlayerControllerB player)
        {
            Zombies.Logger.LogMessage("Blood Dropped Client!");
            player.DropBlood(new Vector3(0, -1, 0));
        }
        /*
        [ServerRpc]
        public void CreateMimicServerRpc(PlayerControllerB player, Vector3 playerPositionAtDeath)
        {
            NetworkManager networkManager = player.NetworkManager;
            if (networkManager == null || !networkManager.IsListening)
                return;
            /*
            if (this.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Server && (networkManager.IsClient || networkManager.IsHost))
            {
                if ((long)this.OwnerClientId != (long)networkManager.LocalClientId)
                {
                    if (networkManager.LogLevel > LogLevel.Normal)
                        return;
                    Debug.LogError((object)"Only the owner can invoke a ServerRpc that requires ownership!");
                    return;
                }
                ServerRpcParams serverRpcParams;
                FastBufferWriter bufferWriter = this.__beginSendServerRpc(1065539967U, serverRpcParams, RpcDelivery.Reliable);
                bufferWriter.WriteValueSafe<bool>(in inFactory, new FastBufferWriter.ForPrimitives());
                bufferWriter.WriteValueSafe(in playerPositionAtDeath);
                this.__endSendServerRpc(ref bufferWriter, 1065539967U, serverRpcParams, RpcDelivery.Reliable);
            }
            */
        /*
            if (!networkManager.IsServer && !networkManager.IsHost)
                return;
            
            if ((UnityEngine.Object)player == (UnityEngine.Object)null)
                Debug.LogError((object)"Previousplayerheldby is null so the mask mimic could not be spawned");
            Debug.Log((object)"Server creating mimic from mask");
            Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(playerPositionAtDeath, sampleRadius: 10f);
            if (RoundManager.Instance.GotNavMeshPositionResult)
            {
                EnemyType mimicEnemy;
                if ((UnityEngine.Object)mimicEnemy == (UnityEngine.Object)null)
                {
                    Debug.Log((object)"No mimic enemy set for mask");
                }
                else
                {
                    NetworkObjectReference netObjectRef = RoundManager.Instance.SpawnEnemyGameObject(navMeshPosition, this.previousPlayerHeldBy.transform.eulerAngles.y, -1, this.mimicEnemy);
                    NetworkObject networkObject;
                    if (netObjectRef.TryGet(out networkObject))
                    {
                        Debug.Log((object)"Got network object for mask enemy");
                        MaskedPlayerEnemy component = networkObject.GetComponent<MaskedPlayerEnemy>();
                        component.SetSuit(this.previousPlayerHeldBy.currentSuitID);
                        component.mimickingPlayer = this.previousPlayerHeldBy;
                        component.SetEnemyOutside(!inFactory);
                        component.SetVisibilityOfMaskedEnemy();
                        component.SetMaskType(this.maskTypeId);
                        this.previousPlayerHeldBy.redirectToEnemy = (EnemyAI)component;
                        if ((UnityEngine.Object)this.previousPlayerHeldBy.deadBody != (UnityEngine.Object)null)
                            this.previousPlayerHeldBy.deadBody.DeactivateBody(false);
                    }
                    this.CreateMimicClientRpc(netObjectRef, inFactory);
                }
            }
            else
                Debug.Log((object)"No nav mesh found; no mimic could be created");
        }
        */
    }
}
