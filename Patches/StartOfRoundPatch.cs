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
            Zombies.ClearZombies();
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
            if (!Zombies.foundMasked)
            {
                foreach (var level in StartOfRound.Instance.levels)
                {
                    Zombies.Logger.LogDebug(level.name);
                    foreach (var enemy in level.Enemies)
                    {
                        Zombies.Logger.LogDebug($"{enemy.enemyType.name}");
                        if (enemy.enemyType.name.Contains("MaskedPlayerEnemy"))
                        {
                            Zombies.foundMasked = true;
                            Zombies.Logger.LogDebug($"Masked Type Found {enemy.enemyType.name}");
                            Zombies.maskEnemy = enemy.enemyType;
                            break;
                            //return;
                        }
                    }
                    if (Zombies.foundMasked)
                    {
                        break;
                    }
                }
            }
            
        }
    }
}
