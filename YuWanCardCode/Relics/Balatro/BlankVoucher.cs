using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;
using YuWanCard.Relics.Balatro;
using YuWanCard.Utils;

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

        List<RelicModel> choices = DeterministicRandomUtils.TakeStableRandom(
            BalatroJokerRelicModel.GetAvailableRewardableJokers(Owner),
            3,
            Owner.RunState.Rng.Niche);
        if (choices.Count == 0)
        {
            return;
        }

        RelicModel? selected = await RelicSelectCmd.FromChooseARelicScreen(Owner, choices);
        if (selected != null)
        {
            await RelicCmd.Obtain(selected.ToMutable(), Owner);
        }
    }
}
