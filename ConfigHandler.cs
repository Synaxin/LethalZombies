using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using BepInEx.Configuration;

namespace Zombies
{
    internal class ConfigHandler 
    {
        
        // We define our config variables in a public scope
        public readonly ConfigEntry<int> tryForTicks;
        public readonly ConfigEntry<float> infectionChance;
        public readonly ConfigEntry<int> proximityChance;
        public readonly ConfigEntry<int> infectionTimeMin;
        public readonly ConfigEntry<int> infectionTimeMax;
        public readonly ConfigEntry<int> wakeTickAmount;
        public readonly ConfigEntry<int> proxWakeTickAmount;
        public readonly ConfigEntry<int> proxHeldWakeTickAmount;
        public readonly ConfigEntry<bool> zombiesDropBodies;
        public readonly ConfigEntry<bool> droppedBodiesInfection;
        public readonly ConfigEntry<bool> infectLivingPlayers;
        public readonly ConfigEntry<float> livingInfectionChance;
        public readonly ConfigEntry<float> livingInfectionModifier;
        public readonly ConfigEntry<bool> reviveOnDeath;
        public readonly ConfigEntry<float> reviveOnDeathChance;
        public readonly ConfigEntry<bool> onlyReviveWhileAlone;


        public ConfigHandler(ConfigFile cfg)
        {

            tryForTicks = cfg.Bind(
                "General",                          // Config section
                "InfectionTickAmount",            // Key of this config
                60,                                 // Default value
                "Amount of ticks to try for infection\nA tick is ~= 2s"    // Description
            );
            infectionChance = cfg.Bind(
                "General",                          // Config section
                "InfectionChance",                  // Key of this config
                0.8f,                               // Default value
                "% Chance per tick of infecting a body"    // Description
            );
            proximityChance = cfg.Bind(
                "General",                          // Config section
                "ProximityChance",                  // Key of this config
                25,                                // Default value
                "% Chance for zombie to be proximity trigger"    // Description
            );
            infectionTimeMin = cfg.Bind(
                "General",                          // Config section
                "InfectionTimeMin",                 // Key of this config
                30,                                 // Default value
                "Minimum ticks before infection is complete\nA tick is ~= 2s"    // Description
            );
            infectionTimeMax = cfg.Bind(
                "General",                          // Config section
                "InfectionTimeMax",                 // Key of this config
                90,                                 // Default value
                "Maximum ticks before infection is complete\nA tick is ~= 2s"    // Description
            );
            wakeTickAmount = cfg.Bind(
                "General",                          // Config section
                "WakeTickAmount",                 // Key of this config
                10,                                 // Default value
                "Amount of ticks a waking zombie will seize\nbefore waking up.\nWake ticks are 0.2s"    // Description
            );
            proxWakeTickAmount = cfg.Bind(
                "General",                          // Config section
                "ProxWakeTickAmount",                 // Key of this config
                10,                                 // Default value
                "Amount of ticks a proximity waking zombie will seize\nbefore waking up.\nWake ticks are 0.2s"    // Description
            );
            proxHeldWakeTickAmount = cfg.Bind(
                "General",                          // Config section
                "ProxHeldWakeTickAmount",                 // Key of this config
                13,                                 // Default value
                "Amount of ticks a held proximity waking zombie will seize\nbefore waking up.\nWake ticks are 0.2s"    // Description
            );
            zombiesDropBodies = cfg.Bind(
                "General",                          // Config section
                "ZombiesDropBodies",                 // Key of this config
                true,                                 // Default value
                "Makes converted/zombified players drop bodies"    // Description
            );
            droppedBodiesInfection = cfg.Bind(
                "General",                          // Config section
                "InfectDeadConverted",                 // Key of this config
                true,                                 // Default value
                "Can dead converted players be infected again\nWill not work without Zombies Drop Bodies"    // Description
            );
            infectLivingPlayers = cfg.Bind(
                "General",                          // Config section
                "InfectLivingPlayers",                 // Key of this config
                true,                                 // Default value
                "Can living players be infected on taking damage"    // Description
            );
            reviveOnDeath = cfg.Bind(
                "MirageLegacyFunction",                          // Config section
                "ReviveOnDeath",                 // Key of this config
                true,                                 // Default value
                "Makes it possible for masked to spawn immediately after player dies"    // Description
            );
            reviveOnDeathChance = cfg.Bind(
                "MirageLegacyFunction",                          // Config section
                "ReviveOnDeathChance",                 // Key of this config
                5f,                                 // Default value
                "Chance for masked to instantly spawn"    // Description
            );
            onlyReviveWhileAlone = cfg.Bind(
                "MirageLegacyFunction",                          // Config section
                "OnlyReviveWhileAlone",                 // Key of this config
                false,                                 // Default value
                "Revive On Death only applies while player is alone"    // Description
            );
            
            /*
            livingInfectionChance = cfg.Bind(
                "General",                          // Config section
                "LivingInfectionChance",                 // Key of this config
                1f,                                 // Default value
                "Chance for living players to be infected"    // Description
            );
            livingInfectionModifier = cfg.Bind(
                "General",                          // Config section
                "LivingInfectionModifier",                 // Key of this config
                0.2f,                                 // Default value
                "Adds (damage * LivingInfectionModifer) to livingInfectionChance\nSet to 0 to disable"    // Description
            );
            */
            // Get rid of old settings from the config file that are not used anymore
            ClearOrphanedEntries(cfg);
            // We need to manually save since we disabled `SaveOnConfigSet` earlier
            cfg.Save();
            // And finally, we re-enable `SaveOnConfigSet` so changes to our config
            // entries are written to the config file automatically from now on
            cfg.SaveOnConfigSet = true;
        }

        static void ClearOrphanedEntries(ConfigFile cfg)
        {
            // Find the private property `OrphanedEntries` from the type `ConfigFile`
            PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
            // And get the value of that property from our ConfigFile instance
            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg);
            // And finally, clear the `OrphanedEntries` dictionary
            orphanedEntries.Clear();
        }
        
    }

}
