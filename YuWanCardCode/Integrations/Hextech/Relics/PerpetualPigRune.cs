using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Hextech;
using YuWanCard.Powers;

namespace YuWanCard.Relics;

public sealed class PerpetualPigRune : HextechPigRuneBase
{
    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Prismatic;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DurationMultiplier", 2)];

    public override Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (Owner == null || power.Owner != Owner.Creature)
        {
            return Task.CompletedTask;
        }

        if (power is PigCoinPower or PigVampiricPower)
        {
            power.SkipNextDurationTick = true;
        }

        return Task.CompletedTask;
    }
}
