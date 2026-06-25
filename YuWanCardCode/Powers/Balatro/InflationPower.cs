using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Abstracts;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public sealed class InflationPower : YuWanPowerModel
{
    private GoldModificationGuard? _goldGuard;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    private GoldModificationGuard GoldGuard
    {
        get
        {
            if (_goldGuard == null || !_goldGuard.IsBoundTo(this))
            {
                _goldGuard = new GoldModificationGuard(
                    this,
                    () => Owner?.Player,
                    amount => Math.Floor(amount * (Amount / 100m)),
                    async (_, _) => { Flash(); await Task.CompletedTask; });
            }

            return _goldGuard;
        }
    }

    public override decimal ModifyGoldGained(Player player, decimal amount)
    {
        return GoldGuard.ModifyGoldGained(player, amount);
    }

    public override async Task AfterModifyingGoldGained(Player player, decimal amount)
    {
        await GoldGuard.AfterModifyingGoldGained(player, amount);
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost + 1m;
        return card.Owner == Owner.Player && originalCost >= 0m && !card.EnergyCost.CostsX;
    }
}
