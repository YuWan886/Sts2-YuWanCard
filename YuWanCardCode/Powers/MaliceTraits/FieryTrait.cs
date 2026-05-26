using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class FieryTrait : MaliceTraitPowerBase
{
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer == null || dealer == Owner || !props.IsPoweredAttack())
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(choiceContext, dealer, 3 * Amount, ValueProp.Unpowered, Owner, null);
    }
}
