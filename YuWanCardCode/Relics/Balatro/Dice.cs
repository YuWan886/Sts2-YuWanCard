using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;
using YuWanCard.Relics.Balatro;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public sealed class Dice : BalatroRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);

        if (Owner == null || player != Owner)
        {
            return;
        }

        BalatroModifier? modifier = GetModifier();
        if (modifier == null)
        {
            return;
        }

        int roll = DeterministicRandomUtils.NextInclusive(Owner.RunState.Rng.CombatCardSelection, 1, 3);
        modifier.SetComboAtLeast(Owner, roll);
    }

    private BalatroModifier? GetModifier()
    {
        return Owner?.RunState is RunState runState
            ? BalatroModifier.GetInstance(runState)
            : null;
    }
}
