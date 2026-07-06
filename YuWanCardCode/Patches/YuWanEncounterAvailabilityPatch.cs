using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using YuWanCard.Config;

namespace YuWanCard.Patches;

internal static class YuWanEncounterAvailabilityHelpers
{
    private static readonly AccessTools.FieldRef<ActModel, RoomSet> RoomsField =
        AccessTools.FieldRefAccess<ActModel, RoomSet>("_rooms");
    private static readonly AccessTools.FieldRef<RoomSet, EncounterModel?> BossField =
        AccessTools.FieldRefAccess<RoomSet, EncounterModel?>("_boss");

    public static IEnumerable<EncounterModel> FilterEnabledEncounters(IEnumerable<EncounterModel>? encounters)
    {
        return encounters?.Where(IsEncounterEnabled) ?? Enumerable.Empty<EncounterModel>();
    }

    public static void SanitizeActRooms(ActModel act)
    {
        RoomSet rooms = RoomsField(act);

        RemoveDisabledEncounters(rooms.normalEncounters);
        RemoveDisabledEncounters(rooms.eliteEncounters);

        EnsureEncounterQueueNotEmpty(
            rooms.normalEncounters,
            FilterEnabledEncounters(act.AllEncounters).Where(static encounter => encounter.RoomType == RoomType.Monster));
        EnsureEncounterQueueNotEmpty(
            rooms.eliteEncounters,
            FilterEnabledEncounters(act.AllEncounters).Where(static encounter => encounter.RoomType == RoomType.Elite));

        SanitizeBosses(act, rooms);
    }

    public static bool IsEncounterEnabled(EncounterModel? encounter)
    {
        return encounter != null && YuWanContentAvailability.IsEncounterTypeEnabled(encounter.GetType());
    }

    private static void RemoveDisabledEncounters(List<EncounterModel> encounters)
    {
        encounters.RemoveAll(static encounter => !IsEncounterEnabled(encounter));
    }

    private static void EnsureEncounterQueueNotEmpty(
        List<EncounterModel> encounters,
        IEnumerable<EncounterModel> fallbackPool)
    {
        if (encounters.Count > 0)
        {
            return;
        }

        foreach (EncounterModel encounter in fallbackPool)
        {
            if (encounters.Any(existing => existing.Id == encounter.Id))
            {
                continue;
            }

            encounters.Add(encounter);
        }
    }

    private static void SanitizeBosses(ActModel act, RoomSet rooms)
    {
        List<EncounterModel> enabledBosses = GetEnabledBossPool(act)
            .GroupBy(static encounter => encounter.Id)
            .Select(static group => group.First())
            .ToList();
        if (enabledBosses.Count == 0)
        {
            return;
        }

        EncounterModel? currentBoss = BossField(rooms);
        EncounterModel? currentSecondBoss = rooms.SecondBoss;
        bool hadSecondBoss = currentSecondBoss != null;

        List<EncounterModel> orderedBosses = [];
        AddIfEnabledDistinct(orderedBosses, currentBoss);
        AddIfEnabledDistinct(orderedBosses, currentSecondBoss);
        foreach (EncounterModel enabledBoss in enabledBosses)
        {
            AddIfDistinct(orderedBosses, enabledBoss);
        }

        rooms.Boss = orderedBosses[0];
        rooms.SecondBoss = hadSecondBoss
            ? orderedBosses.Skip(1).FirstOrDefault(static _ => true)
            : null;
    }

    private static IEnumerable<EncounterModel> GetEnabledBossPool(ActModel act)
    {
        List<EncounterModel> filteredBosses = FilterEnabledEncounters(act.AllBossEncounters).ToList();
        if (filteredBosses.Count > 0)
        {
            return filteredBosses;
        }

        // If a filtered cache was emptied earlier, rebuild from the raw encounter generator so the act still has a boss.
        return FilterEnabledEncounters(act.GenerateAllEncounters()).Where(static encounter => encounter.RoomType == RoomType.Boss);
    }

    private static void AddIfEnabledDistinct(List<EncounterModel> orderedBosses, EncounterModel? encounter)
    {
        if (!IsEncounterEnabled(encounter))
        {
            return;
        }

        AddIfDistinct(orderedBosses, encounter!);
    }

    private static void AddIfDistinct(List<EncounterModel> orderedBosses, EncounterModel encounter)
    {
        if (orderedBosses.Any(existing => existing.Id == encounter.Id))
        {
            return;
        }

        orderedBosses.Add(encounter);
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.AllEncounters), MethodType.Getter)]
public static class YuWanActAllEncountersAvailabilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<EncounterModel> __result)
    {
        __result = YuWanEncounterAvailabilityHelpers.FilterEnabledEncounters(__result).ToList();
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.AllWeakEncounters), MethodType.Getter)]
public static class YuWanActWeakEncountersAvailabilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<EncounterModel> __result)
    {
        __result = YuWanEncounterAvailabilityHelpers.FilterEnabledEncounters(__result).ToList();
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.AllRegularEncounters), MethodType.Getter)]
public static class YuWanActRegularEncountersAvailabilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<EncounterModel> __result)
    {
        __result = YuWanEncounterAvailabilityHelpers.FilterEnabledEncounters(__result).ToList();
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.AllEliteEncounters), MethodType.Getter)]
public static class YuWanActEliteEncountersAvailabilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<EncounterModel> __result)
    {
        __result = YuWanEncounterAvailabilityHelpers.FilterEnabledEncounters(__result).ToList();
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.AllBossEncounters), MethodType.Getter)]
public static class YuWanActBossEncountersAvailabilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<EncounterModel> __result)
    {
        __result = YuWanEncounterAvailabilityHelpers.FilterEnabledEncounters(__result).ToList();
    }
}

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllEncounters), MethodType.Getter)]
public static class YuWanModelDbAllEncountersAvailabilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<EncounterModel> __result)
    {
        __result = YuWanEncounterAvailabilityHelpers.FilterEnabledEncounters(__result).ToList();
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.GenerateRooms))]
public static class YuWanGenerateRoomsEncounterAvailabilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(ActModel __instance)
    {
        YuWanEncounterAvailabilityHelpers.SanitizeActRooms(__instance);
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.ValidateRoomsAfterLoad))]
public static class YuWanValidateRoomsAfterLoadEncounterAvailabilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(ActModel __instance)
    {
        YuWanEncounterAvailabilityHelpers.SanitizeActRooms(__instance);
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.PullNextEncounter))]
public static class YuWanPullNextEncounterAvailabilityPatch
{
    [HarmonyPrefix]
    public static void Prefix(ActModel __instance)
    {
        YuWanEncounterAvailabilityHelpers.SanitizeActRooms(__instance);
    }
}
