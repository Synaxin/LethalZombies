using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;
using static BepInEx.BepInDependency;
using Unity.Netcode;
using Zombies.Scripts;


namespace Zombies;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
//[BepInDependency(StaticNetcodeLib.MyPluginInfo.PLUGIN_GUID, DependencyFlags.HardDependency)]
[BepInDependency(LethalNetworkAPI.MyPluginInfo.PLUGIN_GUID, DependencyFlags.HardDependency)]
public class Zombies : BaseUnityPlugin
{

    public static Zombies Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }
    internal static ConfigHandler BoundConfig { get; private set; } = null!;
    public static EnemyType maskEnemy;

    internal static PlayerControllerB Player { get; set; }
    internal static ZombieNetworkManager Networking { get; set; }

    internal static InfectionHandler Infection { get; set; }




    private void Awake()
    {
        //BoundConfig = new ConfigHandler(base.Config);
        Logger = base.Logger;
        Instance = this;
        BoundConfig = new ConfigHandler(base.Config);

        Patch();

        Logger.LogInfo($"Zombies v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }
}
