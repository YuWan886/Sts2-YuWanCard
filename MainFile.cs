using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using YuWanCard.Badges;
using YuWanCard.Characters;
using YuWanCard.Config;
using YuWanCard.Core.Badges;
using YuWanCard.Core.Interop;
using YuWanCard.Core.Multiplayer;
using YuWanCard.Core.RightClick;
using YuWanCard.Core.Transcendence;
using YuWanCard.Multiplayer;
using YuWanCard.Utils;
using YuWanCard.Hextech;

namespace YuWanCard;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YuWanCard";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static YuWanCardConfig? Config { get; private set; }

    public static void Initialize()
    {
        ModLifecycle.Publish(ModLifecyclePhase.Initializing);

        var patcher = new ModPatcher(ModId);

        // Phase 1: Bulk Harmony patches (auto-discovered via [HarmonyPatch] attributes)
        // Uses PatchAllSafe for per-class try/catch — essential for Android/Mono AOT compatibility.
        // Exclude patches that must be applied conditionally by platform.
        var manualPatches = new HashSet<string>
        {
            nameof(Core.Patches.YuWanDailyRunModifierFilterPatch),
            nameof(Core.Patches.ProgressStateEncounterStatsPatch)
        };
        patcher.PatchAllSafe(Assembly.GetExecutingAssembly(), manualPatches);

        ModLifecycle.Publish(ModLifecyclePhase.PatchesApplied);

        // Phase 2: Platform-conditional patches (wrapped to survive mobile)
        patcher.ApplySingle(
            h => Core.Patches.AutoSlayCharacterPatch.ApplyPatch(h), "AutoSlayCharacter");
        patcher.ApplySingle(
            h => Core.Patches.AutoSlayOptionsPatch.ApplyPatch(h), "AutoSlayOptions");
        patcher.ApplySingle(
            h => Core.Patches.CustomEnergyIconPatches.Apply(h), "CustomEnergyIcons");
        patcher.ApplySingle(
            h => ModInteropProcessor.Process(h, Assembly.GetExecutingAssembly()), "ModInterop");
        patcher.ApplySingle(
            HextechRuntimeCompat.TryInstall, "HextechRuntimeCompat");

        // Desktop-only patches — skip on Android to avoid triggering NDailyRunScreen
        // static constructor which has a known NRE bug on Mono AOT
        if (!IsMobilePlatform())
        {
            patcher.ApplySingle(
                h => h.CreateClassProcessor(typeof(Core.Patches.YuWanDailyRunModifierFilterPatch)).Patch(),
                "YuWanDailyRunModifierFilter");
            patcher.ApplySingle(
                h => h.CreateClassProcessor(typeof(Core.Patches.ProgressStateEncounterStatsPatch)).Patch(),
                "ProgressStateEncounterStats");
        }

        // Phase 3: Content discovery — scan for [Pool] and registration attributes
        ModLifecycle.Publish(ModLifecyclePhase.ContentRegistering);
        ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly());
        SavedPropertyRegistration.RegisterAssembly(Assembly.GetExecutingAssembly());
        TranscendenceRegistry.RegisterDefaults();
        CustomBadgeRegistry.Register((run, playerId) => new PigTycoonBadge(run, playerId));
        ModLifecycle.Publish(ModLifecyclePhase.ContentRegistered);

        // Phase 4: Config, scene conversions, multiplayer, assets
        Config = new YuWanCardConfig();
        ConfigRegistrar.TryDeferredRegister();

        NodeFactory.Init();
        Pig.RegisterScenes();

        TeammatePayMessageHandler.Register();
        SavedPropertySyncMessageHandler.Register();
        YuWanRightClickMessageHandler.Register();

        AssetPreloader.Preload();
        CloudAnalyticsService.Initialize();

        ModLifecycle.Publish(ModLifecyclePhase.Initialized);
        Logger.Info("YuWanCard initialized");
    }

    /// <summary>
    /// Returns true on Android/iOS to gate patches that access types with
    /// broken static constructors on Mono AOT.
    /// </summary>
    private static bool IsMobilePlatform()
    {
        try
        {
            var osName = Godot.OS.GetName();
            return osName == "Android" || osName == "iOS";
        }
        catch
        {
            return false; // Assume desktop if we can't detect
        }
    }
}

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
public static class NMainMenu_ConfigRegisterPatch
{
    public static void Postfix()
    {
        ConfigRegistrar.TryDeferredRegister();
        HextechRuntimeCompat.TryInstallIfAvailable();
    }
}

[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
public static class NGame_Ready_ConfigPreloadPatch
{
    public static void Prefix()
    {
        ConfigRegistrar.TryDeferredRegister();
        HextechRuntimeCompat.TryInstallIfAvailable();
    }
}
