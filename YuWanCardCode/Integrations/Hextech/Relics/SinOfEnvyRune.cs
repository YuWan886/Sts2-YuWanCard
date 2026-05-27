using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Hextech;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

public sealed class SinOfEnvyRune : HextechSharedRuneBase
{
    private readonly Dictionary<ulong, int> _enemyTriggerCounts = [];

    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Gold;

    public override Task BeforeCombatStart()
    {
        _enemyTriggerCounts.Clear();
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player == Owner)
        {
            _enemyTriggerCounts.Clear();
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (Owner == null
            || power.Owner == null
            || power.Owner.Side == Owner.Creature.Side
            || amount <= 0
            || power.Type != PowerType.Buff
            || !PowerSafetyUtils.IsSafePower(power))
        {
            return;
        }

        if (!power.Owner.CombatId.HasValue)
        {
            return;
        }

        ulong key = power.Owner.CombatId.Value;
        int limit = Owner.GetRelic<RingOfSevenCurses>() == null ? 2 : 3;
        _enemyTriggerCounts.TryGetValue(key, out int count);
        if (count >= limit)
        {
            return;
        }

        if (!HextechPigRuneSharedState.RollPercent(this, 0.5f, 0.25f))
        {
            return;
        }

        _enemyTriggerCounts[key] = count + 1;
        Flash();
        await PowerCmd.Apply(ModelDb.GetById<PowerModel>(power.Id).ToMutable(), Owner.Creature, amount, Owner.Creature, null);
    }
}
