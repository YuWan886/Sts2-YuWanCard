using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public sealed class Chip : BalatroRelicModel
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

        modifier.AddCombo(Owner, 3f);
    }

    private BalatroModifier? GetModifier()
    {
        return Owner?.RunState is RunState runState
            ? BalatroModifier.GetInstance(runState)
            : null;
    }
}
