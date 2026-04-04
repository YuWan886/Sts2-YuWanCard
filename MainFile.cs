using BaseLib.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using YuWanCard.Config;
using YuWanCard.Patches;

namespace YuWanCard;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YuWanCard";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static YuWanCardConfig? Config { get; private set; }

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll();
        EndlessModePatch.ApplyMapPointTypeCountsPatches(harmony);
        
        Config = new YuWanCardConfig();
        ModConfigRegistry.Register(ModId, Config);
        Config.ConfigChanged += OnConfigChanged;
        Config.ApplyVSyncSetting();
        
        Logger.Info("YuWanCard initialized");
    }

    private static void OnConfigChanged(object? sender, EventArgs e)
    {
        Config?.ApplyVSyncSetting();
    }
}
