using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Zombies.Scripts;

namespace Zombies.Patches
{
    [HarmonyPatch(typeof(GameNetworkManager))]
    internal class GameNetworkManagerPatch
    {
        [HarmonyPatch("StartHost")]
        [HarmonyPostfix]
        public static void StartHostPost()
        {
            Zombies.Logger.LogDebug("New Infection Handler Made!");
            Zombies.Infection = new InfectionHandler();
            
        }

        [HarmonyPatch("StartDisconnect")]
        [HarmonyPostfix]
        public static void StartDisconnectPost()
        {
            
            if (Zombies.Infection != null)
            {
                Zombies.Infection.Reset();
                Zombies.Infection = null;
            }
        }
    }
}
