using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers;

public class PigExplosionPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool IsInstanced => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(20m, ValueProp.Unpowered)];

    public void SetDamage(decimal damage)
    {
        AssertMutable();
        DynamicVars.Damage.BaseValue = damage;
    }

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
        {
            return;
        }

        if (Amount > 1)
        {
            await PowerCmd.Decrement(this);
            return;
        }

        Flash();
        await Cmd.CustomScaledWait(0.2f, 0.4f);

        foreach (Creature hittableEnemy in CombatState.HittableEnemies)
        {
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NFireSmokePuffVfx.Create(hittableEnemy));
        }

        await Cmd.CustomScaledWait(0.2f, 0.4f);
        await CreatureCmd.Damage(choiceContext, CombatState.HittableEnemies, DynamicVars.Damage, Owner);
        await PowerCmd.Remove(this);
    }
}
