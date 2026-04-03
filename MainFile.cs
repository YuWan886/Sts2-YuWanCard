using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using YuWanCard.Patches;

namespace YuWanCard;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YuWanCard";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll();
        EndlessModePatch.ApplyMapPointTypeCountsPatches(harmony);
        Logger.Info("YuWanCard initialized");
    }
}
