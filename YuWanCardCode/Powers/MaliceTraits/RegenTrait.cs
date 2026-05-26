using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class RegenTrait : MaliceTraitPowerBase
{
    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        int healAmount = Math.Max(1, (int)Math.Ceiling(Owner.MaxHp * 0.1m * Amount));
        Flash();
        await CreatureCmd.Heal(Owner, healAmount, playAnim: true);
    }
}
