using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using System.Reflection;
using YuWanCard.Core.Abstracts;
using YuWanCard.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(WhatIfRelicPool))]
public class WhatIfAllRelics : YuWanRelicModel
{
    private static readonly string[] RewardEffectHookNames =
    [
        "TryModifyCardRewardAlternatives",
        "ShouldAllowSelectingMoreCardRewards",
        "ModifyCardRewardCreationOptions",
        "TryModifyCardRewardOptions",
        "TryModifyRewards"
    ];

    public override RelicRarity Rarity => RelicRarity.Event;

    public WhatIfAllRelics() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner == null)
        {
            return;
        }

        var allCandidateRelics = ModelDb.AllRelics
            .Where(relic => relic.Id != Id && Owner.GetRelicById(relic.Id) == null)
            .ToList();

        var skippedPickupEffectRelics = allCandidateRelics
            .Where(HasPickupEffect)
            .ToList();

        var skippedRewardEffectRelics = allCandidateRelics
            .Where(HasRewardEffect)
            .ToList();

        var relicsToAdd = allCandidateRelics
            .Where(relic => !HasPickupEffect(relic) && !HasRewardEffect(relic))
            .ToList();

        int added = 0;
        int failed = 0;
        int skippedPickupEffect = skippedPickupEffectRelics.Count;
        int skippedRewardEffect = skippedRewardEffectRelics.Count;

        MainFile.Logger.Info($"[WhatIfAllRelics] Preparing relic grant. Candidates={allCandidateRelics.Count}, ToAdd={relicsToAdd.Count}, SkippedPickupEffect={skippedPickupEffect}, SkippedRewardEffect={skippedRewardEffect}");

        if (skippedPickupEffect > 0)
        {
            var previewIds = skippedPickupEffectRelics
                .Take(12)
                .Select(relic => relic.Id.Entry)
                .ToArray();
            MainFile.Logger.Info($"[WhatIfAllRelics] Skipped pickup-effect relics: {string.Join(", ", previewIds)}{(skippedPickupEffectRelics.Count > previewIds.Length ? ", ..." : string.Empty)}");
        }

        if (skippedRewardEffect > 0)
        {
            var previewIds = skippedRewardEffectRelics
                .Except(skippedPickupEffectRelics)
                .Take(12)
                .Select(relic => relic.Id.Entry)
                .ToArray();
            if (previewIds.Length > 0)
            {
                MainFile.Logger.Info($"[WhatIfAllRelics] Skipped reward-effect relics: {string.Join(", ", previewIds)}{(skippedRewardEffectRelics.Count > previewIds.Length ? ", ..." : string.Empty)}");
            }
        }

        foreach (var relicModel in relicsToAdd)
        {
            try
            {
                var relic = relicModel.ToMutable();
                relic.FloorAddedToDeck = 1;
                Owner.AddRelicInternal(relic);
                added++;
            }
            catch (Exception ex)
            {
                failed++;
                MainFile.Logger.Error($"[WhatIfAllRelics] Failed to add {relicModel.Id.Entry}: {ex.Message}");
            }
        }

        MainFile.Logger.Info($"[WhatIfAllRelics] Finished granting relics. Added={added}, Failed={failed}, SkippedPickupEffect={skippedPickupEffect}, SkippedRewardEffect={skippedRewardEffect}");
    }

    private static bool HasPickupEffect(RelicModel relicModel)
    {
        var method = relicModel.GetType().GetMethod(
            nameof(AfterObtained),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        return method != null && method.DeclaringType != null && method.DeclaringType != typeof(RelicModel);
    }

    private static bool HasRewardEffect(RelicModel relicModel)
    {
        var relicType = relicModel.GetType();
        return RewardEffectHookNames.Any(hookName =>
        {
            var method = relicType.GetMethod(hookName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return method != null && method.DeclaringType == relicType;
        });
    }
}
