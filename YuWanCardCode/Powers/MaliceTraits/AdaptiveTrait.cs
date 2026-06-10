using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class AdaptiveTrait : MaliceTraitPowerBase
{
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer == null || dealer == Owner || result.UnblockedDamage <= 0)
        {
            return;
        }

        int currentPlating = Owner.GetPower<PlatingPower>()?.Amount ?? 0;
        int maxPlating = GetMaxPlating(target);
        int toApply = Math.Min((int)Amount, Math.Max(0, maxPlating - currentPlating));
        if (toApply <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<PlatingPower>(new ThrowingPlayerChoiceContext(), Owner, toApply, Owner, null);
    }

    private static int GetMaxPlating(Creature target)
    {
        int actIndex = target.CombatState?.RunState?.CurrentActIndex ?? 0;
        return 3 + Math.Min(Math.Max(actIndex, 0), 2);
    }
}
