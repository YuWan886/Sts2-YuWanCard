using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Modifiers;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public sealed class HolographicJoker : BalatroJokerRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

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

        SerializableCard? previousCard = modifier.PreviousTurnFirstCard;
        if (previousCard == null)
        {
            return;
        }

        CombatState? combatState = player.Creature?.CombatState;
        if (combatState == null)
        {
            return;
        }

        int copyCount = EffectiveCount();
        if (copyCount <= 0)
        {
            return;
        }

        for (int i = 0; i < copyCount; i++)
        {
            CardModel copy = CardModel.FromSerializable(previousCard);
            combatState.AddCard(copy, player);
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, addedByPlayer: true);
        }
    }

    private BalatroModifier? GetModifier()
    {
        return Owner?.RunState is RunState runState
            ? BalatroModifier.GetInstance(runState)
            : null;
    }
}
