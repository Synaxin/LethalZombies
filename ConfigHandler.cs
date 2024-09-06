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
