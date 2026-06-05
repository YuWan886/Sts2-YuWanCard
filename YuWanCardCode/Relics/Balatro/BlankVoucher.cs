using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public sealed class BlankVoucher : BalatroRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner == null)
        {
            return;
        }

        BalatroModifier? modifier = Owner.RunState is RunState runState
            ? BalatroModifier.GetInstance(runState)
            : null;
        if (modifier == null)
        {
            return;
        }

        List<RelicModel> choices =
        [
            ModelDb.Relic<GreedJoker>(),
            ModelDb.Relic<GluttonyJoker>(),
            ModelDb.Relic<MirrorJoker>(),
            ModelDb.Relic<MiserJoker>(),
            ModelDb.Relic<CollectorJoker>(),
            ModelDb.Relic<GamblerJoker>(),
            ModelDb.Relic<PolychromeJoker>(),
            ModelDb.Relic<NegativeJoker>(),
            ModelDb.Relic<LegendJoker>(),
            ModelDb.Relic<HolographicJoker>(),
            ModelDb.Relic<BankerJoker>(),
            ModelDb.Relic<InvestorJoker>()
        ];
        choices = choices
            .OrderBy(_ => Owner.RunState.Rng.Niche.NextFloat())
            .Take(3)
            .ToList();

        RelicModel? selected = await RelicSelectCmd.FromChooseARelicScreen(Owner, choices);
        if (selected != null)
        {
            await RelicCmd.Obtain(selected.ToMutable(), Owner);
        }
    }
}
