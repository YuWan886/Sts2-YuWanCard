using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;
using YuWanCard.Core.Badges;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(BadgePool), nameof(BadgePool.CreateAll))]
public static class BadgePoolPatch
{
    [HarmonyPostfix]
    public static void CreateAllPostfix(SerializableRun run, ulong playerId, ref IReadOnlyCollection<Badge> __result)
    {
        var customBadges = CustomBadgeRegistry.CreateAll(run, playerId);
        if (customBadges.Count == 0) return;

        var allBadges = new List<Badge>(__result);
        allBadges.AddRange(customBadges);
        __result = allBadges.AsReadOnly();
    }
}
