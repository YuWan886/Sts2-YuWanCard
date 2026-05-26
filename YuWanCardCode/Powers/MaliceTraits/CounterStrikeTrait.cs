using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class CounterStrikeTrait : MaliceTraitPowerBase
{
    private bool _shouldCounter;

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        _shouldCounter = false;

        if (target != Owner || dealer == null || dealer == Owner || amount <= 0)
        {
            return 1m;
        }

        float roll = CombatState?.RunState.Rng.Shuffle.NextFloat() ?? 1f;
        if (roll > 0.3f)
        {
            return 1m;
        }

        _shouldCounter = true;
        return 0m;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!_shouldCounter || target != Owner || dealer == null || dealer == Owner)
        {
            return;
        }

        _shouldCounter = false;
        Flash();
        await CreatureCmd.Damage(choiceContext, dealer, 6 * Amount, ValueProp.Unpowered, Owner, null);
    }
}
