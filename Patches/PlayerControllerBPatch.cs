using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using Unity.Netcode;
using Zombies.Scripts;


namespace Zombies.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal static class PlayerControllerBPatch
    {
        /*
        [HarmonyPatch(typeof(PlayerControllerB))]
        internal static class PreloadPatches
        {

            [HarmonyPostfix]
            [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
            private static void ConnectClientToPlayerObjectPatch(PlayerControllerB __instance)
            {
                if (GameNetworkManager.Instance.localPlayerController != __instance) return;

                Zombies.Player = GameNetworkManager.Instance.localPlayerController;
                if (Zombies.Networking != null)
                {
                    Zombies.Networking.Reset();
                    GameObject.Destroy(Zombies.Networking);
                }
                Zombies.Logger.LogDebug("NetworkHandlerAdded!");
                Zombies.Networking = __instance.gameObject.AddComponent<ZombieNetworkManager>();
            }
        }
        */

        [HarmonyPatch("KillPlayer")]
        [HarmonyPostfix]
        public static void KillPlayerPostfix(PlayerControllerB __instance, bool spawnBody)
        {
            Zombies.Logger.LogDebug($"SpawnBody {spawnBody}");
            if (spawnBody)
            {
                Zombies.Logger.LogDebug($"ClientID = {__instance.playerClientId}, ActualID = {__instance.actualClientId}");
                Zombies.Networking.SendDeadMessage(__instance.playerClientId, __instance.actualClientId);
            }
        }

        [HarmonyPatch("SpawnDeadBody")]
        [HarmonyPostfix]
        public static void SpawnDeadBodyPatch(PlayerControllerB __instance)
        {
            if (Zombies.GetConverted(__instance))
            {
                __instance.deadBody.DeactivateBody(false);
                Zombies.TryRemoveConverted(__instance);
            }
            /*
            Zombies.Logger.LogDebug("Check for Zombie -- Spawn Deadbody");
            if (Zombies.Networking.instaSpawnList.ContainsKey(__instance.actualClientId))
            {
                Zombies.Logger.LogDebug("Zombie In Spawnlist -- Spawn Deadbody");
                bool good = false;
                if (Zombies.Networking.instaSpawnList[__instance.actualClientId])
                {
                    Zombies.Logger.LogDebug("Insta Spawn on -- Spawn Deadbody");
                    good = true;
                    if (Zombies.Infection.GetReviveAlone() && !__instance.isPlayerAlone)
                    {
                        Zombies.Logger.LogDebug("Not Alone -- Spawn Deadbody");
                        good = false;
                    }
                }
                if (good)
                {
                    Zombies.Logger.LogDebug("Spawning Zombie -- Spawn Deadbody");
                    __instance.deadBody.DeactivateBody(false);
                }
                
            }
            */
            Zombies.Logger.LogDebug("Dead Body Spawned");
        }

        /*
        [HarmonyPatch("KillPlayerServerRpc")]
        [HarmonyPostfix]
        private static void KillPlayerServerPatch(PlayerControllerB __instance)
        {
            
        }
        */
    }

    [HarmonyPatch(typeof(PlayerControllerB))]
    [HarmonyPatch("KillPlayer")]
    public static class PlayerControllerB_KillPlayer_Patch
    {

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {

            Zombies.Logger.LogDebug("Transpiler fired");

            var codes = new List<CodeInstruction>(instructions);
            var codeToInject = BuildInstructionsToInsert(method, generator);
            Zombies.Logger.LogDebug(codes.Count);
            codes.InsertRange(0, codeToInject);

            return codes.AsEnumerable();
        }

        static List<CodeInstruction>? BuildInstructionsToInsert(MethodBase method, ILGenerator ilGen)
        {
            Zombies.Logger.LogDebug("CodeInstructions constructing");
            var result = new List<CodeInstruction>();

            var argumentIndex_self = 0; // Instance functions are just static functions where the first argument is `self`
            var argumentIndex_spawnBody = 2;
            var argumentIndex_animation = 4;
            var label = ilGen.DefineLabel();

            result.Add(new CodeInstruction(OpCodes.Ldarg, argumentIndex_self));
            result.Add(new CodeInstruction(OpCodes.Call, typeof(Zombies).GetMethod(nameof(Zombies.GetConverted))));
            result.Add(new CodeInstruction(OpCodes.Brfalse_S, label));
            result.Add(new CodeInstruction(OpCodes.Ldarg, argumentIndex_self));
            result.Add(new CodeInstruction(OpCodes.Call, typeof(Zombies).GetMethod(nameof(Zombies.GetConverted))));
            result.Add(new CodeInstruction(OpCodes.Starg_S, argumentIndex_spawnBody));
            result.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
            result.Add(new CodeInstruction(OpCodes.Starg_S, argumentIndex_animation));
            result.Add(new CodeInstruction(OpCodes.Nop));
            result[8].labels.Add(label);


            Zombies.Logger.LogDebug("CodeInstructions Constructed");
            return result;
        }
    }
}
