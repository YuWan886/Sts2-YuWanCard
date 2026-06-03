using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Balatro;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Cards;

[Pool(typeof(BalatroCardPool))]
public sealed class VoidCard : YuWanCardModel
{
    protected override string CardId => "void";

    public VoidCard() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selectable = BalatroCardEditionHelper.GetSelectableHandCards(Owner, this)
            .Where(card => BalatroCardEditionHelper.CanApplyEdition(card, BalatroCardEdition.Negative))
            .ToList();
        if (selectable.Count == 0)
        {
            return;
        }

        CardModel? selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(new LocString("cards", $"{Id.Entry}.selectionScreenPrompt"), 1, 1),
            context: choiceContext,
            player: Owner,
            filter: card => card != this && BalatroCardEditionHelper.CanApplyEdition(card, BalatroCardEdition.Negative),
            source: this)).FirstOrDefault();
        if (selected == null)
        {
            return;
        }

        if (!BalatroCardEditionHelper.TryApplyEdition(selected, BalatroCardEdition.Negative))
        {
            return;
        }

        decimal lossRatio = IsUpgraded ? 0.05m : 0.10m;
        decimal loss = Math.Max(1m, Math.Floor(Owner.Creature.MaxHp * lossRatio));
        await CreatureCmd.LoseMaxHp(choiceContext, Owner.Creature, loss, isFromCard: true);
    }
}
