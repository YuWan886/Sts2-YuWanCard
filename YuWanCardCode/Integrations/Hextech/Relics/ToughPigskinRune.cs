using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Hextech;

namespace YuWanCard.Relics;

public sealed class ToughPigskinRune : HextechPigRuneBase
{
    private int _triggersThisTurn;

    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Silver;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PlatingPower>(1m),
        new DynamicVar("TriggerLimit", 3m)
    ];

    public override Task BeforeCombatStart()
    {
        _triggersThisTurn = 0;
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player == Owner)
        {
            _triggersThisTurn = 0;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power.Owner != Owner?.Creature
            || power is not PlatingPower
            || amount <= 0
            || _triggersThisTurn >= DynamicVars["TriggerLimit"].IntValue)
        {
            return;
        }

        _triggersThisTurn++;
        Flash();
        await PowerCmd.Apply<PlatingPower>(Owner.Creature, DynamicVars["PlatingPower"].BaseValue, Owner.Creature, cardSource);
    }
}
