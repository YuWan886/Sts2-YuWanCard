using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Balatro;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Cards;

[Pool(typeof(BalatroCardPool))]
public sealed class Magician : YuWanCardModel
{
    public Magician() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var options = BalatroCardEditionHelper.GetSelectableHandCards(Owner, this)
            .Where(card => card.IsTransformable)
            .ToList();
        if (options.Count == 0)
        {
            return;
        }

        CardModel? selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(new LocString("cards", $"{Id.Entry}.selectionScreenPrompt"), 1, 1),
            context: choiceContext,
            player: Owner,
            filter: card => card != this && card.IsTransformable,
            source: this)).FirstOrDefault();
        if (selected == null)
        {
            return;
        }

        List<CardModel> sameCost = ModelDb.AllCards
            .Where(card => card.Id != selected.Id
                && card.Rarity is not CardRarity.Status and not CardRarity.Curse and not CardRarity.Token
                && !card.EnergyCost.CostsX
                && card.EnergyCost.Canonical == selected.EnergyCost.Canonical)
            .ToList();
        if (sameCost.Count == 0)
        {
            return;
        }

        CardModel replacement = CardFactory.CreateRandomCardForTransform(
            selected,
            sameCost,
            isInCombat: true,
            Owner.RunState.Rng.CombatCardGeneration);
        await CardCmd.Transform(selected, replacement);
    }
}
