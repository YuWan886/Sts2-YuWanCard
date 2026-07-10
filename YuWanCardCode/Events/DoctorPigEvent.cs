using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Cards.Event;
using YuWanCard.Config;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Events;

public sealed class DoctorPigEvent : YuWanEventModel
{
    private const int KnowledgeChoiceCount = 5;
    private const string InitialDoctorPigPortraitPath = "res://YuWanCard/images/events/doctor_pig1.png";
    private const string HatSnatchedPortraitPath = "res://YuWanCard/images/events/doctor_pig2.png";

    public override ActModel[] Acts => [];

    protected override string? CustomEventImagePath => InitialDoctorPigPortraitPath;

    public override bool IsAllowed(IRunState runState)
    {
        return YuWanContentAvailability.IsEventTypeEnabled<DoctorPigEvent>()
               && runState.CurrentActIndex == 0
               && YuWanColorlessCardCatalog.GetUnlockedDoctorPigCards(runState).Count > 0;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            new EventOption(this, AcceptKnowledgeInheritance, $"{Id.Entry}.pages.INITIAL.options.ACCEPT_KNOWLEDGE_INHERITANCE"),
            new EventOption(
                this,
                SnatchDoctorHat,
                $"{Id.Entry}.pages.INITIAL.options.SNATCH_DOCTOR_HAT",
                HoverTipFactory.FromCardWithCardHoverTips<LeiZhuTi>())
        ];
    }

    private async Task AcceptKnowledgeInheritance()
    {
        var owner = Owner!;
        var colorlessCards = YuWanColorlessCardCatalog.GetUnlockedDoctorPigCards(owner.RunState)
            .ToList();

        if (colorlessCards.Count > 0)
        {
            var creationOptions = CardCreationOptions.ForNonCombatWithDefaultOdds(colorlessCards)
                .WithCustomPool(colorlessCards, CardRarityOddsType.Uniform)
                .WithFlags(CardCreationFlags.NoCardPoolModifications);
            var cards = CardFactory.CreateForReward(owner, Math.Min(KnowledgeChoiceCount, colorlessCards.Count), creationOptions)
                .ToList();

            var prefs = new CardSelectorPrefs(L10NLookup($"{Id.Entry}.pages.ACCEPTED.selectionScreenPrompt"), 1, 1)
            {
                Cancelable = false
            };

            CardModel? selectedCard = (await CardSelectCmd.FromSimpleGridForRewards(
                new BlockingPlayerChoiceContext(),
                cards,
                owner,
                prefs)).FirstOrDefault();

            if (selectedCard != null)
            {
                CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(selectedCard, PileType.Deck));
            }
        }

        SetEventFinished(L10NLookup($"{Id.Entry}.pages.ACCEPTED.description"));
    }

    private async Task SnatchDoctorHat()
    {
        UpdatePortrait(HatSnatchedPortraitPath);

        var card = Owner!.RunState.CreateCard(ModelDb.Card<LeiZhuTi>(), Owner);
        var addResult = await CardPileCmd.Add(card, PileType.Deck);
        if (addResult.success)
        {
            CardCmd.PreviewCardPileAdd(addResult, 2f);
        }

        SetEventFinished(L10NLookup($"{Id.Entry}.pages.HAT_SNATCHED.description"));
    }

    private static void UpdatePortrait(string portraitPath)
    {
        var eventRoom = NEventRoom.Instance;
        if (eventRoom?.Layout == null)
        {
            return;
        }

        eventRoom.SetPortrait(PreloadManager.Cache.GetTexture2D(portraitPath));
    }
}
