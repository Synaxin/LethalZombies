using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
        public static void KillPlayerPostfix(PlayerControllerB __instance, bool spawnBody)
        {
            Zombies.Logger.LogDebug($"SpawnBody {spawnBody}");
            Zombies.Networking.SendDeadMessage(__instance.playerClientId);
        }

        [HarmonyPatch("SpawnDeadBody")]
        [HarmonyPostfix]
        public static void SpawnDeadBodyPatch(PlayerControllerB __instance)
        {
            if (Zombies.GetConverted(__instance))
            {
                __instance.deadBody.DeactivateBody(false);
            }
            Zombies.Logger.LogDebug("Dead Body Spawned");
        }
        
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
