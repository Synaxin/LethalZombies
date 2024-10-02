using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Unity.Netcode;

namespace Zombies.Patches
{

    [HarmonyPatch(typeof(EnemyAI))]
    internal class EnemyAIPatch
    {
        [HarmonyPatch("KillEnemy")]
        [HarmonyPrefix]
        public static void KillEnemyPatch(EnemyAI __instance)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsHost)
            {
                Zombies.Infection.ReplaceDeadBody(__instance.NetworkObject);
            }
        }

        [HarmonyPatch("KillEnemyOnOwnerClient")]
        [HarmonyPostfix]
        private static void KillEnemyOwnerPatch(EnemyAI __instance)
        {
            Zombies.Logger.LogDebug("Enemy Killed OwnerClient");
        }
    }
}
