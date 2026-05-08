using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Utils;

public static class CommonActions
{
    public static AttackCommand CardAttack(CardModel card, CardPlay play, int hitCount = 1)
    {
        decimal damage = card.DynamicVars.Damage.BaseValue;
        var cmd = DamageCmd.Attack(damage).WithHitCount(hitCount).FromCard(card);

        switch (card.TargetType)
        {
            case TargetType.AnyEnemy:
                if (play.Target != null) cmd.Targeting(play.Target);
                break;
            case TargetType.AllEnemies:
                if (card.CombatState != null) cmd.TargetingAllOpponents(card.CombatState);
                break;
            case TargetType.RandomEnemy:
                if (card.CombatState != null) cmd.TargetingRandomOpponents(card.CombatState, true);
                break;
            default:
                if (play.Target != null) cmd.Targeting(play.Target);
                break;
        }

        return cmd;
    }

    public static async Task<decimal> CardBlock(CardModel card, CardPlay? play)
    {
        return await CreatureCmd.GainBlock(card.Owner.Creature, card.DynamicVars.Block, play);
    }

    public static async Task<T?> Apply<T>(PlayerChoiceContext context, Creature target, CardModel card, decimal amount)
        where T : PowerModel
    {
        return await PowerCmd.Apply<T>(context, target, amount, card.Owner.Creature, card);
    }

    public static async Task Apply<T>(PlayerChoiceContext context, IEnumerable<Creature> targets, CardModel card, bool applyToEach = false)
        where T : PowerModel
    {
        decimal amount = card.DynamicVars.Count;
        foreach (var target in targets)
        {
            await PowerCmd.Apply<T>(context, target, amount, card.Owner.Creature, card);
        }
    }

    public static async Task<T?> ApplySelf<T>(PlayerChoiceContext context, CardModel card, decimal amount = 1)
        where T : PowerModel
    {
        return await PowerCmd.Apply<T>(context, card.Owner.Creature, amount, card.Owner.Creature, card);
    }

    public static async Task Draw(CardModel card, PlayerChoiceContext context)
    {
        int amount = card.DynamicVars.Cards.IntValue;
        if (amount <= 0) amount = 1;
        await CardPileCmd.Draw(context, amount, card.Owner);
    }
}
