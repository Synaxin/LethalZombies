using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using HarmonyLib;

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
    }
}
