using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Utils;

namespace YuWanCard.Enchantments;

public sealed class SweepingBlade : CustomEnchantmentModel
{
    public override bool ShowAmount => true;

    public override bool CanEnchant(CardModel card)
    {
        if (!base.CanEnchant(card))
        {
            return false;
        }

        return CardUtils.HasDamageVariable(card);
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (cardSource != Card || dealer == null || target == null)
        {
            return;
        }

        if (!props.IsPoweredAttack())
        {
            return;
        }

        var combatState = dealer.CombatState;
        if (combatState == null)
        {
            return;
        }

        var splashDamage = result.TotalDamage * 0.5m;
        if (splashDamage <= 0)
        {
            return;
        }

        var otherEnemies = combatState.HittableEnemies
            .Where(e => e != target && e.IsAlive)
            .ToList();

        if (otherEnemies.Count == 0)
        {
            return;
        }

        foreach (var enemy in otherEnemies)
        {
            await CreatureCmd.Damage(
                choiceContext,
                enemy,
                splashDamage,
                ValueProp.Unpowered,
                Card
            );
        }
    }
}
