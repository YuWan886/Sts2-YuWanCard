using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Utils;

public static class RelicRewardUtils
{
    public static IEnumerable<RelicModel> GetStandardSharedRewardCandidates(
        IRunState runState,
        IEnumerable<RelicModel> relics)
    {
        return relics.Where(relic => IsStandardSharedRewardCandidate(relic, runState));
    }

    public static bool IsStandardSharedRewardCandidate(RelicModel relic, IRunState runState)
    {
        // Generic shared relic rewards should not leak Balatro-only relics into
        // unrelated reward sources, even if they share the same global pool.
        return relic is not BalatroRelicModel && relic.IsAllowed(runState);
    }

    public static RelicModel? PullNextStandardSharedRelic(Player player)
    {
        int maxAttempts = Math.Max(1, ModelDb.RelicPool<SharedRelicPool>().AllRelics.Count());
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            RelicModel relic = RelicFactory.PullNextRelicFromFront(player);
            if (IsStandardSharedRewardCandidate(relic, player.RunState))
            {
                return relic;
            }
        }

        return null;
    }

    public static RelicReward? CreateStandardSharedRelicReward(Player player)
    {
        RelicModel? relic = PullNextStandardSharedRelic(player);
        return relic == null ? null : new RelicReward(relic.ToMutable(), player);
    }
}
