// Originally developed by Ardot66
// Modified and maintained by Jettcodey
// Licensed under the MIT License
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using REPOLib;
using REPOLib.Modules;
using UnityEngine;

namespace Ardot.Jettcodey.REPO.MapUpgrade;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("REPOLib", BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BaseUnityPlugin
{
    public static new ConfigFile Config;
    public static Harmony Harmony;
    internal static new ManualLogSource Logger;

    public static ConfigEntry<bool> MapRequiresUpgrade;
    public static ConfigEntry<float> MinCost;
    public static ConfigEntry<float> MaxCost;
    public static ConfigEntry<float> MapSizeIncrease;

    private void Awake()
    {
        Logger = base.Logger;
        Config = base.Config;
        Harmony = new(MyPluginInfo.PLUGIN_GUID);

        InitConfig();
        Harmony.PatchAll();
        MapToolChanges.Init();

        Logger.LogInfo(
            $"Plugin {MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded successfully."
        );
        Logger.LogInfo($"Mod originally developed by Ardot66, updated by Jettcodey!");

        string bundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "mapupgrade.file");

        BundleLoader.LoadBundle(
            bundlePath,
            delegate(AssetBundle assetBundle)
            {
                string[] upgrades = new[] { "Map Upgrade" };

                foreach (var upgradeName in upgrades)
                {
                    try
                    {
                        Item item = assetBundle.LoadAsset<Item>(upgradeName);
                        if (item == null)
                        {
                            Logger.LogError(
                                $"Failed loading Item asset '{upgradeName}' from bundle."
                            );
                            continue;
                        }

                        GameObject prefab = assetBundle.LoadAsset<GameObject>(upgradeName);
                        if (prefab == null)
                        {
                            Logger.LogError(
                                $"Failed loading prefab for upgrade '{upgradeName}'. Make sure the prefab is included in the bundle."
                            );
                            continue;
                        }

                        string assetName = $"{upgradeName}";
                        prefab.name = assetName;
                        item.name = assetName;
                        item.itemName = $"{upgradeName}";

                        Value v = ScriptableObject.CreateInstance<Value>();
                        v.valueMin = MinCost.Value / 4f;
                        v.valueMax = MaxCost.Value / 4f;
                        item.value = v;

                        ItemAttributes itemAttributes =
                            prefab.GetComponent<ItemAttributes>()
                            ?? prefab.AddComponent<ItemAttributes>();
                        itemAttributes.item = item;

                        Upgrader upgrader =
                            prefab.GetComponent<Upgrader>() ?? prefab.AddComponent<Upgrader>();

                        var prefabRef = Items.RegisterItem(itemAttributes);
                        if (prefabRef == null)
                        {
                            Logger.LogWarning(
                                $"Items.RegisterItem returned null for '{upgradeName}'."
                            );
                        }
                        else
                        {
                            Logger.LogInfo($"Registered upgrade item '{upgradeName}'.");
                        }

                        Upgrades.RegisterUpgrade(
                            upgradeName,
                            item,
                            (player, level) => UpdatePlayerMapTool(player),
                            null
                        );
                    }
                    catch (System.Exception e)
                    {
                        Logger.LogError(
                            $"Exception while registering upgrade '{upgradeName}': {e}"
                        );
                    }
                }
            },
            false
        );

        Config.SettingChanged += OnConfigChanged;
    }

    private void InitConfig()
    {
        MapRequiresUpgrade = Config.Bind(
            "Upgrade",
            "MapRequiresUpgrade",
            true,
            "If true, one map upgrade is required before the map can be used"
        );
        MinCost = Config.Bind(
            "Upgrade",
            "MinCost",
            7000f,
            new ConfigDescription(
                "The minimum price of a MapUpgrade in the shop",
                new AcceptableValueRange<float>(0f, 50000f)
            )
        );
        MaxCost = Config.Bind(
            "Upgrade",
            "MaxCost",
            10000f,
            new ConfigDescription(
                "The maximum price of a MapUpgrade in the shop",
                new AcceptableValueRange<float>(0f, 50000f)
            )
        );
        MapSizeIncrease = Config.Bind(
            "Upgrade",
            "MapSizeIncrease",
            0.5f,
            new ConfigDescription(
                "The amount that the map increases in size for every map upgrade",
                new AcceptableValueRange<float>(0.1f, 10f)
            )
        );
    }

    private void OnConfigChanged(object sender, SettingChangedEventArgs e)
    {
        if (e.ChangedSetting == MapSizeIncrease || e.ChangedSetting == MapRequiresUpgrade)
        {
            if (PlayerAvatar.instance != null)
            {
                Update_MapUpgrade();
            }
        }
    }

    public static void Update_MapUpgrade()
    {
        if (
            LevelGenerator.Instance.Generated
            && !SemiFunc.MenuLevel()
            && PlayerAvatar.instance != null
        )
        {
            UpdateAllPlayersMapTool();
        }
    }

    public static void UpdateAllPlayersMapTool()
    {
        if (GameDirector.instance == null || GameDirector.instance.PlayerList == null)
            return;

        for (int x = 0; x < GameDirector.instance.PlayerList.Count; x++)
        {
            PlayerAvatar player = GameDirector.instance.PlayerList[x];
            UpdatePlayerMapTool(player);
        }
    }

    public static void UpdatePlayerMapTool(PlayerAvatar player)
    {
        if (player == null || player.mapToolController == null)
            return;

        MapToolChanges mapTool = player.mapToolController.GetComponent<MapToolChanges>();
        if (mapTool == null)
            return;

        int upgrades = Upgrader.GetStat(player.steamID, "playerUpgradeMapUpgrade");

        int effectiveUpgrades = upgrades;
        if (!MapRequiresUpgrade.Value && effectiveUpgrades == 0)
        {
            effectiveUpgrades = 1;
        }

        mapTool.SetMapEnabled(effectiveUpgrades > 0);
        mapTool.SetMaxZoom(
            MapToolChanges.DefaultSize.Value + (effectiveUpgrades - 1) * MapSizeIncrease.Value
        );
    }
}
