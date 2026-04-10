using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigReplay : YuWanCardModel
{
    public PigReplay() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var handCards = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c != this)
            .ToList();

        if (handCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var selectedCards = await CardSelectCmd.FromHand(choiceContext, Owner, prefs, c => c != this, this);

        var selectedCard = selectedCards.FirstOrDefault();
        if (selectedCard != null)
        {
            int replayCount = IsUpgraded ? 2 : 1;
            selectedCard.BaseReplayCount += replayCount;
            MainFile.Logger.Info($"PigReplay: Added Replay {replayCount} to {selectedCard.Id}");
        }
    }
}
