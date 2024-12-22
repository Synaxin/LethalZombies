using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using GameNetcodeStuff;
using System.Collections.Generic;
using UnityEngine;
using static BepInEx.BepInDependency;
using Unity.Netcode;
using Zombies.Scripts;
using System.Linq;
using ModelReplacement;

//Add MoreCompany compat --Already works
//Remnants player body compat?

namespace Zombies;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
//[BepInDependency(StaticNetcodeLib.MyPluginInfo.PLUGIN_GUID, DependencyFlags.HardDependency)]
[BepInDependency(LethalNetworkAPI.MyPluginInfo.PLUGIN_GUID, DependencyFlags.HardDependency)]
[BepInDependency(ModelReplacement.PluginInfo.GUID, DependencyFlags.SoftDependency)]
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

    public static BodySpawnHandler BodySpawn { get; set; }

    internal static ModelReplacementCompat ModelReplaceScript { get; set; }

    public static bool ModelReplacementAPIFound = false;

    public static Dictionary<MaskedPlayerEnemy, bool> ZombieList;
    internal static bool foundMasked = false;




    private void Awake()
    {
        ZombieList = new Dictionary<MaskedPlayerEnemy, bool>();
        //BoundConfig = new ConfigHandler(base.Config);
        Logger = base.Logger;
        Instance = this;
        BoundConfig = new ConfigHandler(base.Config);
        Networking = new ZombieNetworkManager();
        Zombies.BodySpawn = new BodySpawnHandler();
        Patch();
        if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("meow.ModelReplacementAPI"))
        {
            ModelReplacementAPIFound = true;
            ModelReplaceScript = new ModelReplacementCompat();
            Logger.LogMessage("Model Replacement API found");
        }
        Logger.LogInfo($"Zombies v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    public static void AddZombie(MaskedPlayerEnemy mask, bool proximity)
    {
        if (!ZombieList.ContainsKey(mask))
        {
            ZombieList.Add(mask, proximity);
        }
    }

    public static void RemoveZombie(MaskedPlayerEnemy mask)
    {
        if (ZombieList.ContainsKey(mask))
        {
            ZombieList.Remove(mask);
        }
    }

    public static void ClearZombies()
    {
        ZombieList.Clear();
    }

    public static (bool, bool) GetZombie(MaskedPlayerEnemy mask)
    {
        if (ZombieList.ContainsKey(mask))
        {
            return (true, ZombieList[mask]);
        }
        return (false, false);
    }

    public static bool GetConverted(PlayerControllerB player)
    {
        
        if (BodySpawn != null)
        {
            Zombies.Logger.LogDebug($"Got Converted, {BodySpawn.convertedList.Count}");
            return BodySpawn.ContainsPlayer(player);
        }
        return false;
        
    }

    public static void TryRemoveConverted(PlayerControllerB player)
    {
        BodySpawn.RemovePlayer(player);
    }

    public static void ClearConverted()
    {
        BodySpawn.ResetList();
    }

    internal static void SetReplacementModelVisible(PlayerControllerB player)
    {
        Logger.LogDebug("Model Replacement Logic");
        if (!ModelReplacementAPIFound)
        {
            return;
        }
        if (ModelReplaceScript != null)
        {
            ModelReplaceScript.SetBodyVisible(player);
        }
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
