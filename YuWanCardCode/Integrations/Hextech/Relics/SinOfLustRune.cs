using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Hextech;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

public sealed class SinOfLustRune : HextechSharedRuneBase
{
    private bool _spreadUsedThisTurn;
    private int _debuffsAppliedThisTurn;

    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Gold;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DebuffThreshold", 3),
        new PowerVar<DexterityPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DexterityPower>()];

    public override Task BeforeCombatStart()
    {
        _spreadUsedThisTurn = false;
        _debuffsAppliedThisTurn = 0;
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player == Owner)
        {
            _spreadUsedThisTurn = false;
            _debuffsAppliedThisTurn = 0;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (Owner == null || power.Owner == null || amount <= 0 || applier != Owner.Creature || power.Type != PowerType.Debuff)
        {
            return;
        }

        _debuffsAppliedThisTurn++;

        if (!_spreadUsedThisTurn && power.Owner.Side == MegaCrit.Sts2.Core.Combat.CombatSide.Enemy && Owner.Creature.CombatState != null)
        {
            if (!PowerSafetyUtils.IsSafePower(power))
            {
                return;
            }

            _spreadUsedThisTurn = true;
            foreach (Creature enemy in Owner.Creature.CombatState.HittableEnemies.Where(enemy => enemy != power.Owner))
            {
                await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), ModelDb.GetById<PowerModel>(power.Id).ToMutable(), enemy, amount, Owner.Creature, cardSource);
            }
        }

        int threshold = Owner.GetRelic<RingOfSevenCurses>() == null ? DynamicVars["DebuffThreshold"].IntValue : 2;
        if (_debuffsAppliedThisTurn < threshold)
        {
            return;
        }

        _debuffsAppliedThisTurn = 0;
        Flash();
        await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars.Dexterity.BaseValue, Owner.Creature, cardSource);
    }
}
