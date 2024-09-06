using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Unity.Netcode;


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
                    Zombies.Infection.TickThroughBodies();
                }
                //TestMod.Logger.LogMessage("Ticking Host");
                //InfectionHandler.TickThroughBodies();
            }
            
        }

        [HarmonyPatch("ShipHasLeft")]
        [HarmonyPrefix]
        public static void ShipHasLeftPatch()
        {
            if (Zombies.Infection == null)
            {
                return;
            }
            Zombies.Infection.ClearInfected();
        }
    }
}
