using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Powers.MaliceTraits;

/// <summary>
/// 下回合开始时失去能量
/// </summary>
public sealed class CorrosionEnergyLossPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // protected override bool IsVisibleInternal => false;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
            return;

        if (Owner.Player == null)
            return;

        Flash();
        await PlayerCmd.LoseEnergy(Amount, Owner.Player);
        await PowerCmd.Remove(this);
    }
}
