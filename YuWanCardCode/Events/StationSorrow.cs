using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Cards;
using YuWanCard.Cards.Quest;
using MegaCrit.Sts2.Core.Runs;

namespace YuWanCard.Events;

public sealed class StationSorrow : YuWanEventModel
{
    public override ActModel[] Acts => [];

    public override bool IsAllowed(IRunState runState)
    {
        return runState.CurrentActIndex >= 2;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            new EventOption(
                this,
                AddRedKingCard,
                $"{Id.Entry}.pages.INITIAL.options.ADD_RED_KING",
                HoverTipFactory.FromCardWithCardHoverTips<RedKing>()
            ),
            new EventOption(
                this,
                AddPigBurgerCard,
                $"{Id.Entry}.pages.INITIAL.options.ADD_PIG_BURGER",
                HoverTipFactory.FromCardWithCardHoverTips<PigBurger>()
            )
        ];
    }

    private async Task AddRedKingCard()
    {
        var redKingCard = Owner!.RunState.CreateCard(ModelDb.Card<RedKing>(), Owner);
        var addResult = await CardPileCmd.Add(redKingCard, PileType.Deck);

        if (addResult.success)
        {
            CardCmd.PreviewCardPileAdd(addResult, 2f);
        }

        SetEventFinished(L10NLookup($"{Id.Entry}.pages.RED_KING_ADDED.description"));
    }

    private async Task AddPigBurgerCard()
    {
        var pigBurgerCard = Owner!.RunState.CreateCard(ModelDb.Card<PigBurger>(), Owner);
        var addResult = await CardPileCmd.Add(pigBurgerCard, PileType.Deck);

        if (addResult.success)
        {
            CardCmd.PreviewCardPileAdd(addResult, 2f);
        }

        SetEventFinished(L10NLookup($"{Id.Entry}.pages.PIG_BURGER_ADDED.description"));
    }
}
