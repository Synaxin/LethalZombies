using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using Zombies.Scripts;
using Unity.Netcode;

namespace Zombies.Patches
{
    [HarmonyPatch(typeof(Terminal))]

    
    internal class TerminalPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void GetMaskPrefab(ref SelectableLevel[] ___moonsCatalogueList)
        {
            Zombies.BodySpawn = new BodySpawnHandler();
            foreach (var enemyType in Resources.FindObjectsOfTypeAll<EnemyType>().Distinct())
            {

                if (enemyType.name == "MaskedPlayerEnemy")
                {
                    Zombies.Logger.LogDebug("Masked Type Found");
                    Zombies.maskEnemy = enemyType;
                    return;
                }
            }
            
        }
    }
}
