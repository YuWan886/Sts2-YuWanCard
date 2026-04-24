using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Utils;

namespace YuWanCard.Enchantments;

public sealed class Venomous : YuWanEnchantmentModel
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

        if (result.TotalDamage <= 0)
        {
            return;
        }

        await PowerCmd.Apply<PoisonPower>(choiceContext, target, 3, dealer, Card);
    }
}
