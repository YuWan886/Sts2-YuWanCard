using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Utils;

namespace YuWanCard.Enchantments;

public sealed class ArthropodKiller : YuWanEnchantmentModel
{
    public override bool ShowAmount => true;

    private decimal _storedDamage;
    private bool _shouldBypassBlock;

    public override bool CanEnchant(CardModel card)
    {
        if (!base.CanEnchant(card))
        {
            return false;
        }

        return CardUtils.HasDamageVariable(card);
    }

    public override Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        _storedDamage = 0m;
        _shouldBypassBlock = false;

        if (cardSource != Card || dealer != Card?.Owner?.Creature)
        {
            return Task.CompletedTask;
        }

        if (!props.IsPoweredAttack())
        {
            return Task.CompletedTask;
        }

        if (!ArthropodUtils.IsArthropod(target))
        {
            return Task.CompletedTask;
        }

        if (target.Block <= 0)
        {
            return Task.CompletedTask;
        }

        _storedDamage = amount;
        _shouldBypassBlock = true;

        return Task.CompletedTask;
    }

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!_shouldBypassBlock)
        {
            return amount;
        }

        return _storedDamage;
    }

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!_shouldBypassBlock)
        {
            return Task.CompletedTask;
        }

        if (result.BlockedDamage > 0)
        {
            target.GainBlockInternal(result.BlockedDamage);
        }

        _shouldBypassBlock = false;

        return Task.CompletedTask;
    }
}
