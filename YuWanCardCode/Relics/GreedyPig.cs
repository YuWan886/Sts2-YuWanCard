using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Cards;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class GreedyPig : YuWanRelicModel
{
    [SavedProperty]
    private bool HasAddedGreedThisCombat { get; set; }

    private decimal _pendingGoldBonus;
    private bool _isApplyingBonus;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public GreedyPig() : base(true)
    {
    }

    public override RelicModel? GetUpgradeReplacement() => null;

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
        {
            return Task.CompletedTask;
        }

        HasAddedGreedThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        if (HasAddedGreedThisCombat || Owner?.Creature?.CombatState == null)
        {
            return;
        }

        HasAddedGreedThisCombat = true;
        Flash();

        CardModel greedCard = ModelDb.Card<Greed>();
        CardModel card = Owner.Creature.CombatState.CreateCard(greedCard, Owner);

        var hand = PileType.Hand.GetPile(Owner);
        bool isHandFull = hand.Cards.Count >= 10;

        if (isHandFull)
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
            MainFile.Logger.Info($"GreedyPig: Added Greed to top of draw pile (hand full)");
        }
        else
        {
            var results = await CardPileCmd.AddGeneratedCardsToCombat([card], PileType.Hand, addedByPlayer: true);

            if (results.Count > 0 && results[0].success)
            {
                MainFile.Logger.Info($"GreedyPig: Added Greed to hand");
            }
            else
            {
                MainFile.Logger.Warn($"GreedyPig: Failed to add Greed to hand");
            }
        }
    }

    public override bool ShouldGainGold(decimal amount, Player player)
    {
        if (_isApplyingBonus)
        {
            return true;
        }
        if (player != Owner)
        {
            return true;
        }
        _pendingGoldBonus = Math.Floor(amount * 0.5m);
        return true;
    }

    public override async Task AfterGoldGained(Player player)
    {
        if (player == Owner && !_isApplyingBonus && _pendingGoldBonus > 0m)
        {
            decimal bonus = _pendingGoldBonus;
            _pendingGoldBonus = 0m;
            _isApplyingBonus = true;
            Flash();
            await PlayerCmd.GainGold(bonus, player);
            _isApplyingBonus = false;
        }
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        await base.AfterCombatVictory(room);
        HasAddedGreedThisCombat = false;
    }
}
