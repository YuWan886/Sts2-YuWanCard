using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

/// <summary>
/// Every 3rd attack card played: gain 5 gold.
/// </summary>
[Pool(typeof(SharedRelicPool))]
public sealed class GreedJoker : BalatroJokerRelicModel
{
    private const int GoldPerTrigger = 5;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(context, cardPlay);

        if (Owner == null || cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Attack)
        {
            return;
        }

        BalatroModifier? modifier = GetModifier();
        if (modifier == null)
        {
            return;
        }

        if (modifier.AttackCardsThisTurn % 3 == 0)
        {
            int multiplier = EffectiveCount();
            await PlayerCmd.GainGold(GoldPerTrigger * multiplier, Owner);
        }
    }

    private int EffectiveCount()
    {
        int count = 1;
        if (Owner != null && Owner.GetRelic<Blueprint>() != null)
        {
            count *= 2;
        }
        return count;
    }

    private BalatroModifier? GetModifier()
    {
        return Owner?.RunState is RunState runState
            ? BalatroModifier.GetInstance(runState)
            : null;
    }
}
