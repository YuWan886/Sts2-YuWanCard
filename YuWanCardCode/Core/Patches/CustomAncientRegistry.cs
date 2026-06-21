using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using YuWanCard.Core.Abstracts;

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

/// <summary>
/// The relic compendium only builds ancient subcategories for ancients present in
/// the unlock state's shared-ancient set. Include custom ancients there so their
/// relic options can be listed under the Ancient relic page.
/// </summary>
[HarmonyPatch(typeof(UnlockState), nameof(UnlockState.SharedAncients), MethodType.Getter)]
static class UnlockStateSharedAncientsPatch
{
    [HarmonyPostfix]
    static IEnumerable<AncientEventModel> AddCustomUnlockedAncients(IEnumerable<AncientEventModel> __result)
    {
        return __result
            .Concat(CustomAncientRegistry.CustomAncients)
            .Distinct();
    }
}

/// <summary>
/// Custom ancients can constrain themselves to a specific act. Keep shared-ancient
/// distribution consistent with that rule when RunManager fans shared ancients out
/// across later acts.
/// </summary>
[HarmonyPatch(typeof(ActModel), nameof(ActModel.SetSharedAncientSubset))]
static class ActSharedAncientSubsetFilterPatch
{
    [HarmonyPrefix]
    static void FilterInvalidCustomAncients(ActModel __instance, ref List<AncientEventModel> sharedAncientSubset)
    {
        if (sharedAncientSubset.Count == 0)
        {
            return;
        }

        ActModel act = __instance.CanonicalInstance;
        sharedAncientSubset = sharedAncientSubset
            .Where(ancient => ancient is not YuWanAncientModel customAncient || customAncient.IsValidForAct(act))
            .ToList();
    }
}
