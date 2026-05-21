using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Powers;

public class HealingPotionPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(5m)];

    private int HealPerTurn => DynamicVars["Heal"].IntValue;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner.Player != player || Owner.IsDead)
        {
            return;
        }

        Flash();
        await CreatureCmd.Heal(Owner, HealPerTurn);
        await PowerCmd.Decrement(this);
    }
}
