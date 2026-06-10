using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using YuWanCard.Core.Persistence;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class ReincarnatedEye : YuWanRelicModel
{
    private static readonly SavedAttachedState<ReincarnatedEye, bool> HasTriggeredThisCombatState =
        new(nameof(HasTriggeredThisCombat));

    private static readonly SavedAttachedState<ReincarnatedEye, bool> HasAddedCardThisCombatState =
        new(nameof(HasAddedCardThisCombat));

    private bool HasTriggeredThisCombat
    {
        get => HasTriggeredThisCombatState.GetValueOrDefault(this, false);
        set => HasTriggeredThisCombatState[this] = value;
    }

    private bool HasAddedCardThisCombat
    {
        get => HasAddedCardThisCombatState.GetValueOrDefault(this, false);
        set => HasAddedCardThisCombatState[this] = value;
    }

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
        var owner = Owner;
        if (owner == null || player != owner)
        {
            return;
        }

        if (HasTriggeredThisCombat || HasAddedCardThisCombat)
        {
            return;
        }

        if (owner.Creature?.CombatState == null)
        {
            return;
        }

        var deck = owner.Deck;

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

        var prompt = new LocString("relics", $"{Id.Entry}.selectionPrompt");
        var selectedCards = await CardSelectCmd.FromDeckGeneric(
            owner,
            new CardSelectorPrefs(prompt, 1),
            filter: FilterCard
        );

        var cardToCopy = selectedCards.FirstOrDefault();
        if (cardToCopy == null)
        {
            return;
        }

        var combatState = owner.Creature?.CombatState;
        if (combatState == null)
        {
            return;
        }

        CardModel copiedCard = CardModel.FromSerializable(cardToCopy.ToSerializable());
        combatState.AddCard(copiedCard, owner);

        // 检查手牌是否已满（最大手牌数为 10）
        var hand = PileType.Hand.GetPile(owner);
        bool isHandFull = hand.Cards.Count >= 10;

        if (isHandFull)
        {
            // 手牌已满，将卡牌放置到抽牌堆顶部
            CardPileAddResult addResult = await CardPileCmd.Add(copiedCard, PileType.Draw, CardPilePosition.Top);
            CardCmd.PreviewCardPileAdd(addResult);
            MainFile.Logger.Info($"ReincarnatedEye: Copied {cardToCopy.Title} to top of draw pile (hand full)");
        }
        else
        {
            // 手牌未满，直接加入手牌
            await CardPileCmd.AddGeneratedCardToCombat(copiedCard, PileType.Hand, Owner);
            MainFile.Logger.Info($"ReincarnatedEye: Copied {cardToCopy.Title} to hand");
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
