using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(RegentRelicPool))]
public class HaotianHammer : YuWanRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<SovereignBlade>()];

    public HaotianHammer() : base(true)
    {
    }

    public override RelicModel? GetUpgradeReplacement() => null;

    public override Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (Owner == null || card is not SovereignBlade blade || card.IsUpgraded)
        {
            return Task.CompletedTask;
        }

        var combatState = Owner.Creature?.CombatState;
        if (combatState == null)
        {
            return Task.CompletedTask;
        }

        foreach (var player in combatState.Players)
        {
            var hand = player.PlayerCombatState?.Hand;
            if (hand == null || !hand.Cards.Contains(card))
            {
                continue;
            }

            MainFile.Logger.Info($"HaotianHammer: SovereignBlade added to {player.Creature.Name}'s hand, upgrading it");
            CardCmd.Upgrade(blade);
            
            if (player == Owner)
            {
                Flash();
            }
        }

        return Task.CompletedTask;
    }
}
