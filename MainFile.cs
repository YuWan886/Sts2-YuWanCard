using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YuWanCard.Badges;
using YuWanCard.Config;
using YuWanCard.Core.Badges;
using YuWanCard.Core.Interop;
using YuWanCard.Core.Lifecycle;
using YuWanCard.Core.Patching;
using YuWanCard.Core.Registration;
using YuWanCard.Multiplayer;
using YuWanCard.Patches;
using YuWanCard.Utils;

namespace YuWanCard;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YuWanCard";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static YuWanCardConfig? Config { get; private set; }

    private const string PigVisualsPath = "res://YuWanCard/scenes/characters/pig.tscn";
    private const string PigMerchantPath = "res://YuWanCard/scenes/characters/pig_merchant.tscn";
    private const string PigEnergyCounterPath = "res://YuWanCard/scenes/characters/pig_energy_counter.tscn";
    private const string PigRestSitePath = "res://YuWanCard/scenes/rest_site/characters/pig_rest_site.tscn";

    public static void Initialize()
    {
        ModLifecycle.Publish(ModLifecyclePhase.Initializing);

        var patcher = new ModPatcher(ModId);

        // Phase 1: Bulk Harmony patches (auto-discovered via [HarmonyPatch] attributes)
        patcher.ApplySingle(
            h => h.PatchAll(Assembly.GetExecutingAssembly()), "BulkPatchAll");

        ModLifecycle.Publish(ModLifecyclePhase.PatchesApplied);

        // Phase 2: Platform-conditional patches (wrapped to survive mobile)
        patcher.ApplySingle(
            h => EndlessModePatch.ApplyMapPointTypeCountsPatches(h), "EndlessMode");
        patcher.ApplySingle(
            h => Core.Patches.AutoSlayCharacterPatch.ApplyPatch(h), "AutoSlayCharacter");
        patcher.ApplySingle(
            h => Core.Patches.AutoSlayOptionsPatch.ApplyPatch(h), "AutoSlayOptions");
        patcher.ApplySingle(
            h => ModInteropProcessor.Process(h, Assembly.GetExecutingAssembly()), "ModInterop");

        // Phase 3: Content discovery — scan for [Pool] and registration attributes
        ModLifecycle.Publish(ModLifecyclePhase.ContentRegistering);
        ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly());
        SavedPropertyRegistration.RegisterAssembly(Assembly.GetExecutingAssembly());
        CustomBadgeRegistry.Register((run, playerId) => new PigTycoonBadge(run, playerId));
        ModLifecycle.Publish(ModLifecyclePhase.ContentRegistered);

        // Phase 4: Config, scene conversions, multiplayer, assets
        Config = new YuWanCardConfig();
        ConfigRegistrar.TryDeferredRegister();

        NodeFactory.Init();
        RegisterSceneConversions();

        TeammatePayMessageHandler.Register();

        PreloadAssets();

        ModLifecycle.Publish(ModLifecyclePhase.Initialized);
        Logger.Info("YuWanCard initialized");
    }

    private static void RegisterSceneConversions()
    {
        NodeFactory.RegisterSceneType<NCreatureVisuals>(PigVisualsPath);
        NodeFactory.RegisterSceneType<NMerchantCharacter>(PigMerchantPath);
        NodeFactory.RegisterSceneType<NEnergyCounter>(PigEnergyCounterPath);
        NodeFactory.RegisterSceneType<NRestSiteCharacter>(PigRestSitePath);
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

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
public static class NMainMenu_ConfigRegisterPatch
{
    public static void Postfix()
    {
        ConfigRegistrar.TryDeferredRegister();
    }
}

[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
public static class NGame_Ready_ConfigPreloadPatch
{
    public static void Prefix()
    {
        ConfigRegistrar.TryDeferredRegister();
    }
}
