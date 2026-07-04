using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Unlocks;
using YuWanCard.Config;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Patches;

internal static class YuWanAncientAvailabilityHelpers
{
    private static readonly AccessTools.FieldRef<ActModel, RoomSet> RoomsField =
        AccessTools.FieldRefAccess<ActModel, RoomSet>("_rooms");

    private static readonly AccessTools.FieldRef<ActModel, List<AncientEventModel>?> SharedAncientSubsetField =
        AccessTools.FieldRefAccess<ActModel, List<AncientEventModel>?>("_sharedAncientSubset");

    public static IEnumerable<AncientEventModel> FilterEnabledSharedAncients(IEnumerable<AncientEventModel>? ancients)
    {
        return ancients?.Where(static ancient => ancient != null && YuWanContentAvailability.IsAncientTypeEnabled(ancient.GetType()))
               ?? Enumerable.Empty<AncientEventModel>();
    }

    public static void SanitizeActAncient(ActModel act, UnlockState? unlockState = null)
    {
        RoomSet rooms = RoomsField(act);
        if (!rooms.HasAncient)
        {
            return;
        }
        
        AncientEventModel currentAncient = rooms.Ancient;
        if (IsAncientEnabled(currentAncient, act))
        {
            return;
        }

        AncientEventModel? replacement = GetReplacementAncient(act, unlockState);
        if (replacement == null)
        {
            return;
        }

        rooms.Ancient = replacement;
    }

    private static AncientEventModel? GetReplacementAncient(ActModel act, UnlockState? unlockState)
    {
        unlockState ??= RunManager.Instance?.State?.UnlockState;

        IEnumerable<AncientEventModel> actAncients = unlockState != null
            ? act.GetUnlockedAncients(unlockState)
            : act.AllAncients;

        IEnumerable<AncientEventModel> sharedAncients = SharedAncientSubsetField(act) ?? [];

        return actAncients
            .Concat(sharedAncients)
            .Where(candidate => IsAncientEnabled(candidate, act))
            .GroupBy(static ancient => ancient.Id)
            .Select(static group => group.First())
            .FirstOrDefault();
    }

    private static bool IsAncientEnabled(AncientEventModel? ancient, ActModel act)
    {
        if (ancient == null || !YuWanContentAvailability.IsAncientTypeEnabled(ancient.GetType()))
        {
            return false;
        }

        if (ancient is YuWanAncientModel yuWanAncient && !yuWanAncient.IsValidForAct(act.CanonicalInstance))
        {
            return false;
        }

        return true;
    }
}

[HarmonyPriority(Priority.Last)]
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllSharedAncients), MethodType.Getter)]
public static class YuWanSharedAncientAvailabilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<AncientEventModel> __result)
    {
        __result = YuWanAncientAvailabilityHelpers.FilterEnabledSharedAncients(__result).ToList();
    }
}

[HarmonyPriority(Priority.Last)]
[HarmonyPatch(typeof(UnlockState), nameof(UnlockState.SharedAncients), MethodType.Getter)]
public static class YuWanUnlockStateSharedAncientAvailabilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<AncientEventModel> __result)
    {
        __result = YuWanAncientAvailabilityHelpers.FilterEnabledSharedAncients(__result).ToList();
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
public static class YuWanGenerateRoomsAncientAvailabilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(ActModel __instance, UnlockState unlockState)
    {
        YuWanAncientAvailabilityHelpers.SanitizeActAncient(__instance, unlockState);
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.ValidateRoomsAfterLoad))]
public static class YuWanValidateRoomsAfterLoadAncientAvailabilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(ActModel __instance)
    {
        YuWanAncientAvailabilityHelpers.SanitizeActAncient(__instance);
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.PullAncient))]
public static class YuWanPullAncientAvailabilityPatch
{
    [HarmonyPrefix]
    public static void Prefix(ActModel __instance)
    {
        YuWanAncientAvailabilityHelpers.SanitizeActAncient(__instance);
    }
}
