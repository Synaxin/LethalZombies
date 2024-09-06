using System;
using System.Collections.Generic;
using System.Text;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using Zombies.Scripts;


namespace Zombies.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal static class PlayerControllerBPatch
    {
        [HarmonyPatch(typeof(PlayerControllerB))]
        internal static class PreloadPatches
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
            private static void ConnectClientToPlayerObjectPatch(PlayerControllerB __instance)
            {
                if (GameNetworkManager.Instance.localPlayerController != __instance) return;
                Zombies.Player = GameNetworkManager.Instance.localPlayerController;
                Zombies.Logger.LogDebug("NetworkHandlerAdded!");
                
                Zombies.Networking = __instance.gameObject.AddComponent<ZombieNetworkManager>();
            }
        }

        [HarmonyPatch("KillPlayer")]
        [HarmonyPostfix]
        public static void KillPlayerPatch(PlayerControllerB __instance)
        {
            Zombies.Networking.SendDeadMessage(__instance.playerClientId);
        }
        
    }
}
