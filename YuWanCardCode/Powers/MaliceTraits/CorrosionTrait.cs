using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class CorrosionTrait : MaliceTraitPowerBase
{
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner || target.Player == null || target.IsDead || result.UnblockedDamage <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<CorrosionEnergyLossPower>(new ThrowingPlayerChoiceContext(), target, Amount, Owner, null);
    }
}
