using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class SlownessTrait : MaliceTraitPowerBase
{
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner || target.IsDead || !target.IsPlayer || result.UnblockedDamage <= 0)
        {
            return;
        }

        int currentFrail = target.GetPower<FrailPower>()?.Amount ?? 0;
        int maxFrail = GetMaxFrailAmount();
        int toApply = Math.Min((int)Amount, Math.Max(0, maxFrail - currentFrail));
        if (toApply <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), target, toApply, Owner, null);
    }

    private int GetMaxFrailAmount() => Math.Min(4, Math.Max(1, (int)Amount) + 1);
}
