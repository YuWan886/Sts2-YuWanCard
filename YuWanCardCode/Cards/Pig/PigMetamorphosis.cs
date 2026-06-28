using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigMetamorphosis : YuWanCardModel
{
    public PigMetamorphosis() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
        WithKeyword(CardKeyword.Exhaust, UpgradeType.Remove);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(new LocString("cards", $"{Id.Entry}.selectionScreenPrompt"), 1, 1),
            context: choiceContext,
            player: Owner,
            filter: card => card != this && card.IsTransformable,
            source: this)).FirstOrDefault();
        if (selected == null)
        {
            return;
        }

        CardModel? replacement = CardUtils.CreateRandomTransformCard(
            selected,
            Owner,
            [
                ModelDb.CardPool<PigCardPool>(),
                ModelDb.CardPool<ColorlessCardPool>()
            ],
            upgradeResult: true);
        if (replacement == null)
        {
            return;
        }

        await CardCmd.Transform(selected, replacement);
    }
}
