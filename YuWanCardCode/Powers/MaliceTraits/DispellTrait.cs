using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class DispellTrait : MaliceTraitPowerBase
{
    public override Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return Task.CompletedTask;
        }

        // Disable all enchantments on players' cards
        foreach (var player in combatState.Players)
        {
            foreach (var card in player.PlayerCombatState?.AllCards ?? Enumerable.Empty<CardModel>())
            {
                if (card.Enchantment != null)
                {
                    card.Enchantment.Status = EnchantmentStatus.Disabled;
                }
            }
        }

        Flash();
        return Task.CompletedTask;
    }
}
