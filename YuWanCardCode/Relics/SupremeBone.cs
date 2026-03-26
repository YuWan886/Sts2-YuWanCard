using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class SupremeBone : YuWanRelicModel
{
    [SavedProperty]
    private bool HasTriggeredLowHpEffectThisCombat { get; set; }

    [SavedProperty]
    private bool HasAddedCardThisCombat { get; set; }

    [SavedProperty]
    private bool ShouldTriggerDelayedEffect { get; set; }

    private List<string> SelectedCardIds { get; set; } = new();

    public override RelicRarity Rarity => RelicRarity.Shop;

    public SupremeBone() : base(true)
    {
    }

    public override RelicModel? GetUpgradeReplacement() => null;

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
        {
            return Task.CompletedTask;
        }

        HasAddedCardThisCombat = false;
        HasTriggeredLowHpEffectThisCombat = false;
        ShouldTriggerDelayedEffect = false;
        SelectedCardIds.Clear();
        return Task.CompletedTask;
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner == null || HasAddedCardThisCombat)
        {
            return;
        }

        if (Owner.Deck == null || Owner.Deck.Cards.Count == 0)
        {
            return;
        }

        HasAddedCardThisCombat = true;

        Flash();

        var prompt = new LocString("relics", "YUWANCARD-SUPREME_BONE.selectionPrompt");
        var selectedCards = (await CardSelectCmd.FromDeckGeneric(
            Owner,
            new CardSelectorPrefs(prompt, 2),
            filter: FilterCard
        )).ToList();

        if (selectedCards.Count == 0)
        {
            return;
        }

        SelectedCardIds = selectedCards.Select(c => c.Id.Entry).ToList();
        MainFile.Logger.Info($"SupremeBone: Selected {selectedCards.Count} cards: {string.Join(", ", SelectedCardIds)}");

        var combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            MainFile.Logger.Warn($"SupremeBone: CombatState is null");
            return;
        }

        var allCards = PileType.Draw.GetPile(Owner).Cards
            .Concat(PileType.Hand.GetPile(Owner).Cards)
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Concat(PileType.Exhaust.GetPile(Owner).Cards)
            .ToList();

        MainFile.Logger.Info($"SupremeBone: Found {allCards.Count} cards in combat piles");

        foreach (var card in allCards)
        {
            if (SelectedCardIds.Contains(card.Id.Entry))
            {
                CardCmd.ApplyKeyword(card, CardKeyword.Exhaust);
                MainFile.Logger.Info($"SupremeBone: Added Exhaust to {card.Title} (Id: {card.Id.Entry}, Keywords: {string.Join(", ", card.Keywords)})");
            }
        }
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        return Task.CompletedTask;
    }

    private bool FilterCard(CardModel c)
    {
        return true;
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature.Player != Owner || HasTriggeredLowHpEffectThisCombat || ShouldTriggerDelayedEffect)
        {
            return Task.CompletedTask;
        }

        if (Owner.Creature.CurrentHp <= Owner.Creature.MaxHp * 0.3m)
        {
            HasTriggeredLowHpEffectThisCombat = true;
            ShouldTriggerDelayedEffect = true;
            Flash();
            MainFile.Logger.Info($"SupremeBone: HP dropped below 30%, will trigger effect at next player turn start");
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || !ShouldTriggerDelayedEffect)
        {
            return;
        }

        ShouldTriggerDelayedEffect = false;
        Flash();

        MainFile.Logger.Info($"SupremeBone: Triggering delayed effect - gain 2 energy and draw 3 cards");
        await PlayerCmd.GainEnergy(2, Owner);
        await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), 3, Owner);
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        await base.AfterCombatVictory(room);
        HasTriggeredLowHpEffectThisCombat = false;
        HasAddedCardThisCombat = false;
        ShouldTriggerDelayedEffect = false;
    }
}
