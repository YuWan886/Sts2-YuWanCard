using YuWanCard.Core.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Registers custom ancient (Neow bonus) models with the game.
/// on ModelDb.AllSharedAncients to include custom ancients.
/// </summary>
public static class CustomAncientRegistry
{
    /// <summary>
    /// All custom ancient instances. Populated by YuWanAncientModel constructor.
    /// </summary>
    public static readonly List<AncientEventModel> CustomAncients = [];

    /// <summary>
    /// Called from YuWanAncientModel constructor or [RegisterAncient] attribute processing.
    /// After ContentRegistry is frozen, logs a warning and skips.
    /// </summary>
    public static void Register(AncientEventModel ancient)
    {
        if (ContentRegistry.IsFrozen)
        {
            MainFile.Logger.Warn(
                $"CustomAncientRegistry: Register called after freeze for {ancient.GetType().Name}");
            return;
        }

        if (!CustomAncients.Contains(ancient))
            CustomAncients.Add(ancient);
    }
}

/// <summary>
/// Adds custom ancients to the game's shared ancient list so they appear
/// in the compendium and on the map.
/// </summary>
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllSharedAncients), MethodType.Getter)]
static class AllSharedAncientsPatch
{
    [HarmonyPostfix]
    static IEnumerable<AncientEventModel> AddCustomAncients(IEnumerable<AncientEventModel> __result)
    {
        return [.. __result, .. CustomAncientRegistry.CustomAncients];
    }
}
