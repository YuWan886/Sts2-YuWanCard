using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace YuWanCard.Powers;

public class ZhuGePigPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ZhuGePigPower", 3m)];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        Flash();

        await CardPileCmd.ShuffleIfNecessary(choiceContext, player);

        var drawPile = PileType.Draw.GetPile(player);
        var topCards = drawPile.Cards.Take(Amount).ToList();

        if (topCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(
            new LocString("powers", "YUWANCARD-ZHU_GE_PIG_POWER.selectionScreenPrompt"),
            0,
            topCards.Count
        );

        var cardsToDiscard = (await CardSelectCmd.FromSimpleGrid(
            choiceContext, topCards, player, prefs
        )).ToList();

        var cardsToKeep = topCards.Except(cardsToDiscard).ToList();

        // Return kept cards to top of draw pile in original order
        foreach (var card in cardsToKeep)
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
        }

        // Move selected cards to discard
        if (cardsToDiscard.Count > 0)
        {
            await CardCmd.Discard(choiceContext, cardsToDiscard);
        }
    }
}
