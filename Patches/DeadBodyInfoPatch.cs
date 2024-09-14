using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;


namespace Zombies.Patches
{
    [HarmonyPatch(typeof(DeadBodyInfo))]
    internal class DeadBodyInfoPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StartPatch(DeadBodyInfo __instance)
        {
            Zombies.Logger.LogDebug($"DeadBodyStartPost {__instance.gameObject.transform.position}");
            for (int index = 0; index < __instance.playerScript.bodyParts.Length; ++index)
            {
                Zombies.Logger.LogDebug($"Bodypart {__instance.bodyParts[index].name} Position {__instance.bodyParts[index].position}");
            }
        }
    }
}
