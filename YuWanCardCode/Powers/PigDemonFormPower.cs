using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Monsters;
using YuWanCard.Utils;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

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

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side) return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner, Amount * StrengthGain, Owner, null);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        var pigMinion = PetManager.FindPetByType<PigMinion>(oldOwner);
        CreatureVisualUtils.ResetPigTransformationVisuals(oldOwner, pigMinion);

        await Task.CompletedTask;
    }
}
