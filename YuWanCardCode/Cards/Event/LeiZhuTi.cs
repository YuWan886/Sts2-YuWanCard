using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Config;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Cards.Event;

[Pool(typeof(EventCardPool))]
public sealed class LeiZhuTi : YuWanCardModel
{
    private const int CardChoiceCount = 3;

    public LeiZhuTi() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Event,
        target: TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var colorlessCards = YuWanColorlessCardCatalog.GetUnlockedDoctorPigCards(Owner)
            .ToList();

        if (colorlessCards.Count == 0)
        {
            return;
        }

        var creationOptions = CardCreationOptions.ForNonCombatWithDefaultOdds(colorlessCards)
            .WithCustomPool(colorlessCards, CardRarityOddsType.Uniform)
            .WithFlags(CardCreationFlags.NoCardPoolModifications);
        var cards = CardFactory.CreateForReward(Owner, Math.Min(CardChoiceCount, colorlessCards.Count), creationOptions)
            .ToList();

        if (IsUpgraded)
        {
            foreach (var result in cards)
            {
                if (result.Card.IsUpgradable)
                {
                    CardCmd.Upgrade(result.Card);
                }
            }
        }

        var prefs = new CardSelectorPrefs(new LocString("cards", $"{Id.Entry}.selectionScreenPrompt"), 1, 1);
        CardModel? selectedCard = (await CardSelectCmd.FromSimpleGridForRewards(choiceContext, cards, Owner, prefs))
            .FirstOrDefault();
        if (selectedCard == null)
        {
            return;
        }

        selectedCard.EnergyCost.SetCustomBaseCost(0);
        await CardPileCmd.AddGeneratedCardToCombat(selectedCard, PileType.Hand, Owner);
    }
}
