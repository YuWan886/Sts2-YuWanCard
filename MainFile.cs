using BaseLib.Config;
using BaseLib.Utils;
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

        RegisterAudioReplacements();
        
        Logger.Info("YuWanCard initialized");
    }

    private static void RegisterAudioReplacements()
    {
        var modDir = Path.GetDirectoryName(typeof(MainFile).Assembly.Location);
        if (modDir == null) return;

        var soundsDir = Path.Combine(modDir, "sounds");

#pragma warning disable CS0618
        var wipeSound = Path.Combine(soundsDir, "wipe_yuwancard-pig.mp3");
        if (File.Exists(wipeSound))
        {
            FmodAudio.RegisterFileReplacement("event:/sfx/ui/wipe_yuwancard-pig", wipeSound);
        }

        var dieSound = Path.Combine(soundsDir, "pig_die.mp3");
        if (File.Exists(dieSound))
        {
            FmodAudio.RegisterFileReplacement("event:/sfx/characters/yuwancard-pig/yuwancard-pig_die", dieSound);
        }
#pragma warning restore CS0618
    }

    private static void OnConfigChanged(object? sender, EventArgs e)
    {
        
    }
}
