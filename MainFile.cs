using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YuWanCard.Config;
using YuWanCard.Multiplayer;
using YuWanCard.Patches;
using YuWanCard.Utils;

namespace YuWanCard;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YuWanCard";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static YuWanCardConfig? Config { get; private set; }

    private const string PigVisualsPath = "res://YuWanCard/scenes/characters/pig.tscn";
    private const string PigMerchantPath = "res://YuWanCard/scenes/characters/pig_merchant.tscn";
    private const string PigEnergyCounterPath = "res://YuWanCard/scenes/characters/pig_energy_counter.tscn";
    private const string PigRestSitePath = "res://YuWanCard/scenes/rest_site/characters/pig_rest_site.tscn";

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        EndlessModePatch.ApplyMapPointTypeCountsPatches(harmony);
        AutoSlayCharacterPatch.ApplyPatch(harmony);
        AutoSlayOptionsPatch.ApplyPatch(harmony);

        // Register all models with [Pool] attribute before ModelDb initializes
        ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly());

        try
        {
            Config = new YuWanCardConfig();
            RegisterConfig();
        }
        catch (Exception ex)
        {
            Logger.Warn($"Config init failed (BaseLib not available?): {ex.Message}");
            Config = null;
        }

        NodeFactory.Init();
        RegisterSceneConversions();

        TeammatePayMessageHandler.Register();

        PreloadAssets();

        Logger.Info("YuWanCard initialized");
    }

    private static void RegisterConfig()
    {
        try
        {
            var baseLibConfigType = Type.GetType("BaseLib.Config.ModConfigRegistry, BaseLib");
            if (baseLibConfigType != null)
            {
                var registerMethod = baseLibConfigType.GetMethod("Register");
                registerMethod?.Invoke(null, [ModId, Config]);
                Logger.Info("Registered config via BaseLib");
            }
        }
        catch (Exception)
        {
            Logger.Warn("BaseLib not available; config UI disabled");
        }
    }

    private static void PreloadAssets()
    {
        VfxUtils.PreloadScenes(
            "res://YuWanCard/scenes/vfx/vfx_blood_wheel_eye.tscn",
            "res://YuWanCard/scenes/vfx/vfx_black_hole.tscn",
            "res://YuWanCard/scenes/vfx/vfx_glitch.tscn",
            "res://YuWanCard/scenes/vfx/vfx_glass_shatter.tscn",
            "res://YuWanCard/scenes/vfx/vfx_matrix_rain.tscn"
        );

        VfxUtils.PreloadFrames("res://YuWanCard/images/vfx/blood_wheel_eye/blood_wheel_eye", 48);

        PreloadTextures(
            "res://YuWanCard/images/characters/character_icon_pig.png",
            "res://YuWanCard/images/powers/pig_doubt_power.png"
        );
    }

    private static void RegisterSceneConversions()
    {
        NodeFactory.RegisterSceneType<NCreatureVisuals>(PigVisualsPath);
        NodeFactory.RegisterSceneType<NMerchantCharacter>(PigMerchantPath);
        NodeFactory.RegisterSceneType<NEnergyCounter>(PigEnergyCounterPath);
        NodeFactory.RegisterSceneType<NRestSiteCharacter>(PigRestSitePath);
    }

    private static void OnConfigChanged(object? sender, EventArgs e)
    {
    }

    private static void PreloadTextures(params string[] texturePaths)
    {
        int loadedCount = 0;
        foreach (var path in texturePaths)
        {
            if (ResourceLoader.Exists(path))
            {
                ResourceLoader.Load<Texture2D>(path);
                loadedCount++;
            }
            else
            {
                Logger.Warn($"PreloadTextures: Texture not found: {path}");
            }
        }
        if (loadedCount > 0)
        {
            Logger.Debug($"PreloadTextures: Preloaded {loadedCount} textures");
        }
    }
}

