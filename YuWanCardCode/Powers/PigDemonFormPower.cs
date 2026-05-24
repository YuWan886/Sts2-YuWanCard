using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Monsters;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class PigDemonFormPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("StrengthGain", 2m)
    ];

    private int StrengthGain => DynamicVars["StrengthGain"].IntValue;

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != Owner.Side) return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(Owner, Amount * StrengthGain, Owner, null);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        CreatureVisualUtils.SwitchCreatureSkin(oldOwner, "normal");

        var pigMinion = PetManager.FindPetByType<PigMinion>(oldOwner);
        if (pigMinion != null && pigMinion.IsAlive)
        {
            CreatureVisualUtils.SwitchCreatureSkin(pigMinion, "normal");
        }

        await Task.CompletedTask;
    }
}
