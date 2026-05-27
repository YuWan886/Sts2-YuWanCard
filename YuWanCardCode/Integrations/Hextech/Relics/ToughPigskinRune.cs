using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Hextech;

namespace YuWanCard.Relics;

public sealed class ToughPigskinRune : HextechPigRuneBase
{
    private int _triggersThisCombat;

    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Silver;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PlatingPower>(1m),
        new DynamicVar("TriggerLimit", 5m)
    ];

    public override Task BeforeCombatStart()
    {
        _triggersThisCombat = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (Owner == null
            || creature != Owner.Creature
            || amount <= 0
            || _triggersThisCombat >= DynamicVars["TriggerLimit"].IntValue)
        {
            return;
        }

        _triggersThisCombat++;
        Flash();
        await PowerCmd.Apply<PlatingPower>(Owner.Creature, DynamicVars["PlatingPower"].BaseValue, Owner.Creature, cardSource);
    }
}
