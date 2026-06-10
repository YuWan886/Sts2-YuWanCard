using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Relics.Balatro;

public abstract class BalatroJokerRelicModel : BalatroRelicModel
{
    protected int EffectiveCount()
    {
        if (Owner == null)
        {
            return 0;
        }

        return 1 + (Owner.GetRelic<Blueprint>()?.CopiesJoker(this) == true ? 1 : 0);
    }

    public static List<RelicModel> GetAvailableRewardableJokers(Player player)
    {
        return GetRewardableJokers()
            .Where(relic => !player.Relics.Any(existing => existing.Id == relic.Id))
            .ToList();
    }

    public static IEnumerable<RelicModel> GetRewardableJokers()
    {
        yield return ModelDb.Relic<GreedJoker>();
        yield return ModelDb.Relic<GluttonyJoker>();
        yield return ModelDb.Relic<MirrorJoker>();
        yield return ModelDb.Relic<MiserJoker>();
        yield return ModelDb.Relic<CollectorJoker>();
        yield return ModelDb.Relic<GamblerJoker>();
        yield return ModelDb.Relic<PolychromeJoker>();
        yield return ModelDb.Relic<NegativeJoker>();
        yield return ModelDb.Relic<LegendJoker>();
        yield return ModelDb.Relic<HolographicJoker>();
        yield return ModelDb.Relic<BankerJoker>();
        yield return ModelDb.Relic<InvestorJoker>();
    }
}
