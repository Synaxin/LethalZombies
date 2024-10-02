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
            
            /*
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
             */

        }

    }
}
