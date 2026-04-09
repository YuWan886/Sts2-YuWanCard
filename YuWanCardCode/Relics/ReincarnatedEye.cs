using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class ReincarnatedEye : YuWanRelicModel
{
    [SavedProperty]
    private bool HasTriggeredThisCombat { get; set; }

    [SavedProperty]
    private bool HasAddedCardThisCombat { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public ReincarnatedEye() : base(true)
    {
    }

    public override RelicModel? GetUpgradeReplacement() => null;

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
        {
            return Task.CompletedTask;
        }

        // 重置战斗标记
        HasAddedCardThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        if (HasTriggeredThisCombat || HasAddedCardThisCombat)
        {
            return;
        }

        if (Owner?.Creature?.CombatState == null)
        {
            return;
        }

        var combatState = Owner.Creature.CombatState;
        var deck = Owner.Deck;

        if (deck == null || deck.Cards.Count == 0)
        {
            return;
        }

        var availableCards = deck.Cards.ToList();

        if (availableCards.Count == 0)
        {
            return;
        }

        HasTriggeredThisCombat = true;
        HasAddedCardThisCombat = true;

        Flash();

        var prompt = new LocString("relics", "YUWANCARD-REINCARNATED_EYE.selectionPrompt");
        var selectedCards = await CardSelectCmd.FromDeckGeneric(
            Owner,
            new CardSelectorPrefs(prompt, 1),
            filter: FilterCard
        );

        var cardToCopy = selectedCards.FirstOrDefault();
        if (cardToCopy == null)
        {
            return;
        }

        CardModel copiedCard = combatState.CreateCard(cardToCopy.CanonicalInstance, Owner);

        if (cardToCopy.IsUpgraded)
        {
            for (int i = 0; i < cardToCopy.CurrentUpgradeLevel; i++)
            {
                CardCmd.Upgrade(copiedCard);
            }
        }

        // 检查手牌是否已满（最大手牌数为 10）
        var hand = PileType.Hand.GetPile(Owner);
        bool isHandFull = hand.Cards.Count >= 10;

        if (isHandFull)
        {
            // 手牌已满，将卡牌放置到抽牌堆顶部
            await CardPileCmd.Add(copiedCard, PileType.Draw, CardPilePosition.Top);
            MainFile.Logger.Info($"ReincarnatedEye: Copied {cardToCopy.Title} to top of draw pile (hand full)");
        }
        else
        {
            // 手牌未满，直接加入手牌
            var results = await CardPileCmd.AddGeneratedCardsToCombat([copiedCard], PileType.Hand, addedByPlayer: true);

            if (results.Count > 0 && results[0].success)
            {
                MainFile.Logger.Info($"ReincarnatedEye: Copied {cardToCopy.Title} to hand");
            }
            else
            {
                MainFile.Logger.Warn($"ReincarnatedEye: Failed to add copied card to hand");
            }
        }
    }

    private bool FilterCard(CardModel c)
    {
        return true;
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        await base.AfterCombatVictory(room);
        HasTriggeredThisCombat = false;
    }
}
