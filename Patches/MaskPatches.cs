using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using Unity.Netcode;
using GameNetcodeStuff;
using System.Reflection;
using System.Reflection.Emit;

namespace Zombies.Patches
{
    [HarmonyPatch(typeof(MaskedPlayerEnemy))]
    internal class MaskedPlayerEnemyPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void StartPatch(MaskedPlayerEnemy __instance)
        {
            __instance.timeSinceSpawn = 4;
            //__instance.creatureAnimator.SetBool("Stunned", true); //If this doesn't work try SetTrigger("HitEnemy")
            __instance.creatureAnimator.SetTrigger("HitEnemy");
            __instance.SwitchToBehaviourState(0);
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        private static void UpdatePatch(MaskedPlayerEnemy __instance)
        {
            if (__instance.lastPlayerKilled != null)
            {
                __instance.lastPlayerKilled = null;
            }
            if (__instance.playersKilled.Count > 0)
            {
                __instance.playersKilled.Clear();
            }
        }

        [HarmonyPatch("KillPlayerAnimationClientRpc")]
        [HarmonyPostfix]
        private static void KillPlayerClientPatch(MaskedPlayerEnemy __instance)
        {
            Zombies.Logger.LogMessage("Added Body to list!");
            Zombies.BodySpawn.AddBody(__instance.inSpecialAnimationWithPlayer);
        }

        [HarmonyPatch("CreateMimicClientRpc")]
        [HarmonyPrefix]
        private static void CreateMimicPatch(
            MaskedPlayerEnemy __instance,
            NetworkObjectReference netObjectRef,
            bool inFactory,
            int playerKilled)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Zombies.Logger.LogDebug($"Dead From Masked {netObjectRef}, {inFactory}, {playerKilled}");
                if (StartOfRound.Instance.ClientPlayerList.ContainsKey((ulong)__instance.inSpecialAnimationWithPlayer.actualClientId))
                {
                    Zombies.Infection.AddZombie(netObjectRef, StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[(ulong)__instance.inSpecialAnimationWithPlayer.actualClientId]]);
                }
            }
            
        }

    }


    [HarmonyPatch(typeof(MaskedPlayerEnemy))]
    [HarmonyPatch("SetVisibilityOfMaskedEnemy")]
    public static class SetVisPatch
    {

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {

            Zombies.Logger.LogDebug("Transpiler fired Vispatch");

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
            var label = ilGen.DefineLabel();

            result.Add(new CodeInstruction(OpCodes.Ldarg, argumentIndex_self));
            result.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.enemyEnabled))));
            result.Add(new CodeInstruction(OpCodes.Brtrue_S, label));
            result.Add(new CodeInstruction(OpCodes.Ldarg, argumentIndex_self));
            result.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
            result.Add(new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(MaskedPlayerEnemy), nameof(MaskedPlayerEnemy.enemyEnabled))));
            result.Add(new CodeInstruction(OpCodes.Ldarg, argumentIndex_self));
            result.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
            result.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
            result.Add(new CodeInstruction(OpCodes.Callvirt, typeof(MaskedPlayerEnemy).GetMethod(nameof(MaskedPlayerEnemy.EnableEnemyMesh))));
            result.Add(new CodeInstruction(OpCodes.Ret));
            result[10].labels.Add(label);


            Zombies.Logger.LogDebug("CodeInstructions Constructed");
            return result;
        }
    }


    [HarmonyPatch(typeof(HauntedMaskItem))]
    internal class HauntedMaskItemPatch
    {
        [HarmonyPatch("CreateMimicClientRpc")]
        [HarmonyPrefix]
        private static void CreateMimicPatch(
            NetworkObjectReference netObjectRef,
            bool inFactory,
            HauntedMaskItem __instance)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Zombies.Logger.LogDebug($"Dead From HauntedMask {netObjectRef}, {inFactory}, {__instance.previousPlayerHeldBy.playerClientId}");
                if (StartOfRound.Instance.ClientPlayerList.ContainsKey((ulong)__instance.previousPlayerHeldBy.actualClientId))
                {
                    Zombies.Infection.AddZombie(netObjectRef, StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[(ulong)__instance.previousPlayerHeldBy.actualClientId]]);
                }
            }
            
        }

        [HarmonyPatch("FinishAttaching")]
        [HarmonyPrefix]
        private static void AttachPatch(HauntedMaskItem __instance)
        {
            if ((__instance.IsOwner && !__instance.finishedAttaching) && __instance.previousPlayerHeldBy.AllowPlayerDeath())
            {
                Zombies.Logger.LogMessage("Added Body to list!");
                if (StartOfRound.Instance.ClientPlayerList.ContainsKey((ulong)__instance.previousPlayerHeldBy.actualClientId))
                {
                    Zombies.BodySpawn.AddBody(StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[(ulong)__instance.previousPlayerHeldBy.actualClientId]]);
                }
            }
        }
    }
}
