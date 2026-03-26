using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Relics;

[Pool(typeof(RegentRelicPool))]
public class HaotianHammer : YuWanRelicModel
{
    [SavedProperty]
    private bool HasUpgradedBladesThisCombat { get; set; }

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<SovereignBlade>()];

    public HaotianHammer() : base(true)
    {
    }

    public override RelicModel? GetUpgradeReplacement() => null;

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
        {
            return Task.CompletedTask;
        }

        HasUpgradedBladesThisCombat = false;
        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        if (Owner == null || HasUpgradedBladesThisCombat)
        {
            return Task.CompletedTask;
        }

        HasUpgradedBladesThisCombat = true;
        UpgradeSovereignBladesInAllHands();
        return Task.CompletedTask;
    }

    private void UpgradeSovereignBladesInAllHands()
    {
        if (Owner?.Creature?.CombatState == null)
        {
            return;
        }

        var combatState = Owner.Creature.CombatState;
        var players = combatState.Players;

        foreach (var player in players)
        {
            var hand = player.PlayerCombatState?.Hand;
            if (hand == null)
            {
                continue;
            }

            var sovereignBlades = hand.Cards
                .Where(c => c is SovereignBlade && !c.IsUpgraded)
                .ToList();

            foreach (var blade in sovereignBlades)
            {
                CardCmd.Upgrade(blade);
            }

            if (sovereignBlades.Count > 0 && player == Owner)
            {
                Flash();
                MainFile.Logger.Info($"HaotianHammer: Upgraded {sovereignBlades.Count} SovereignBlade(s) in hand");
            }
        }
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        await base.AfterCombatVictory(room);
        HasUpgradedBladesThisCombat = false;
    }
}
