using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class ChefPigPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ChefPigPower", 1m)];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        var transformableCards = PileType.Hand.GetPile(player).Cards
            .Where(c => c.IsTransformable)
            .ToList();

        if (transformableCards.Count == 0) return;

        Flash();

        int count = Math.Min(Amount, transformableCards.Count);
        var prefs = new CardSelectorPrefs(
            new LocString("powers", $"{Id.Entry}.selectionScreenPrompt"),
            count
        );
        var selectedCards = (await CardSelectCmd.FromHand(
            context: choiceContext,
            player: player,
            prefs: prefs,
            filter: c => c.IsTransformable,
            source: this
        )).ToList();

        foreach (var selectedCard in selectedCards)
        {
            var cardScope = selectedCard.CardScope;
            if (cardScope == null) continue;

            var canonicalFoodCard = CardUtils.GetRandomFoodPigCardCanonical(player);
            var replacementCard = cardScope.CreateCard(canonicalFoodCard, player);
            await CardCmd.Transform(selectedCard, replacementCard);
        }
    }
}
