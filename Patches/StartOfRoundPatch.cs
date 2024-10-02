using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using System.Linq;


namespace Zombies.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePost()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                if (Zombies.Infection != null)
                {
                    Zombies.Infection.TickThroughRevivals();
                    Zombies.Infection.TickThroughBodies();
                }
                //TestMod.Logger.LogMessage("Ticking Host");
                //InfectionHandler.TickThroughBodies();
            }
            
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void AwakePost()
        {
            Zombies.Logger.LogMessage("StartOfRound Awake!");
            Zombies.ClearConverted();
        }

        [HarmonyPatch("ShipHasLeft")]
        [HarmonyPrefix]
        public static void ShipHasLeftPatch()
        {
            if (Zombies.Infection != null)
            {
                Zombies.Infection.Reset();
                Zombies.Infection.ClearInfected();
                //Zombies.Infection.RollInstaSpawn();
            }
            Zombies.ClearConverted();
        }
    }

    [HarmonyPatch(typeof(RoundManager))]
    internal class RoundManagerPatch
    {
        [HarmonyPatch("LoadNewLevel")]
        [HarmonyPrefix]
        private static void LoadLevelPatch()
        {
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
            Zombies.Infection.RollInstaSpawn();
            //if (Zombies.maskEnemy == null)
            foreach (var enemyType in Resources.FindObjectsOfTypeAll<EnemyType>().Distinct())
            {
                Zombies.Logger.LogDebug($"{enemyType.name}");
                if (enemyType.name == "MaskedPlayerEnemy")
                {
                    Zombies.Logger.LogDebug($"Enemytype prefab {enemyType.enemyPrefab.name} {enemyType.enemyPrefab} name {enemyType.enemyName} hash {enemyType.enemyPrefab.GetHashCode()}");
                    Zombies.Logger.LogDebug($"Masked Type Found {enemyType.name}");
                    Zombies.maskEnemy = enemyType;
                    //return;
                }
            }
        }
    }
}
