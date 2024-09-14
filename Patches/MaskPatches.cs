using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using Unity.Netcode;
using GameNetcodeStuff;

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
            NetworkObjectReference netObjectRef,
            bool inFactory,
            int playerKilled)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Zombies.Logger.LogDebug($"Dead From Masked {netObjectRef}, {inFactory}, {playerKilled}");
                Zombies.Infection.AddZombie(netObjectRef, StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[(ulong)playerKilled]]);
            }
            
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
                Zombies.Infection.AddZombie(netObjectRef, StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[(ulong)__instance.previousPlayerHeldBy.playerClientId]]);
            }
            
        }

        [HarmonyPatch("FinishAttaching")]
        [HarmonyPrefix]
        private static void AttachPatch(HauntedMaskItem __instance)
        {
            if ((__instance.IsOwner && !__instance.finishedAttaching) && __instance.previousPlayerHeldBy.AllowPlayerDeath())
            {
                Zombies.Logger.LogMessage("Added Body to list!");
                Zombies.BodySpawn.AddBody(StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[(ulong)__instance.previousPlayerHeldBy.playerClientId]]);
            }
        }
    }
}
