using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public sealed class PigFortuneTelling : YuWanCardModel
{
    protected override bool IsPlayable =>
        base.IsPlayable && GoldSpendHelper.CanAfford(Owner, DynamicVars.Gold.IntValue);

    protected override bool ShouldGlowRedInHand =>
        Owner != null && Owner.Gold < DynamicVars.Gold.IntValue;

    private static readonly LocString SelectionPrompt =
        new("cards", "YUWANCARD-PIG_FORTUNE_TELLING.selectionScreenPrompt");

    public PigFortuneTelling() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithVars(new GoldVar(8));
        WithVar("PreviewCount", 5, 2);
        WithVar("SelectCount", 1, 1);
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
        {
            return;
        }

        if (!await GoldSpendHelper.TrySpend(Owner, DynamicVars.Gold.IntValue, nameof(PigFortuneTelling)))
        {
            return;
        }

        await CardPileCmd.ShuffleIfNecessary(choiceContext, Owner);

        var drawPileCards = PileType.Draw.GetPile(Owner).Cards.ToList();
        if (drawPileCards.Count == 0)
        {
            return;
        }

        int previewCount = Math.Min(DynamicVars["PreviewCount"].IntValue, drawPileCards.Count);
        int selectCount = Math.Min(DynamicVars["SelectCount"].IntValue, previewCount);

        var randomCards = drawPileCards
            .ToList()
            .UnstableShuffle(Owner.RunState.Rng.CombatCardGeneration)
            .Take(previewCount)
            .ToList();

        var prefs = new CardSelectorPrefs(SelectionPrompt, 0, selectCount);
        var selectedCards = (await CardSelectCmd.FromSimpleGrid(choiceContext, randomCards, Owner, prefs)).ToList();

        foreach (var card in selectedCards)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }
}
