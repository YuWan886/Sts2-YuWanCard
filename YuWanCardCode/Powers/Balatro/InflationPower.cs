using MegaCrit.Sts2.Core.Commands;
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

    private GoldModificationGuard GoldGuard => _goldGuard ??= new GoldModificationGuard(
        () => Owner.Player,
        amount => Math.Floor(amount * (Amount / 100m)),
        async amount => await PlayerCmd.GainGold(amount, Owner.Player!));

    public override bool ShouldGainGold(decimal amount, Player player)
    {
        return GoldGuard.ShouldGainGold(amount, player);
    }

    public override async Task AfterGoldGained(Player player)
    {
        await GoldGuard.AfterGoldGained(player);
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost + 1m;
        return card.Owner == Owner.Player && originalCost >= 0m && !card.EnergyCost.CostsX;
    }
}
