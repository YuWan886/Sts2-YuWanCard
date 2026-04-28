using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class ChefPigPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("PigChefPower", 1m)];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        var transformableCards = PileType.Hand.GetPile(player).Cards
            .Where(c => c.IsTransformable)
            .ToList();

        if (transformableCards.Count == 0) return;

        Flash();

        int count = Math.Min(Amount, transformableCards.Count);
        var transformations = new List<CardTransformation>();
        var selectedCardSet = new HashSet<CardModel>();

        for (int i = 0; i < count; i++)
        {
            var remaining = PileType.Hand.GetPile(player).Cards
                .Where(c => c.IsTransformable && !selectedCardSet.Contains(c))
                .ToList();

            if (remaining.Count == 0) break;

            List<CardModel> selectedCards;
            if (remaining.Count == 1)
            {
                selectedCards = remaining;
            }
            else
            {
                var prefs = new CardSelectorPrefs(
                    new LocString("powers", "YUWANCARD-PIG_CHEF_POWER.selectionScreenPrompt"),
                    1
                );
                selectedCards = (await CardSelectCmd.FromHand(
                    context: choiceContext,
                    player: player,
                    prefs: prefs,
                    filter: c => c.IsTransformable && !selectedCardSet.Contains(c),
                    source: this
                )).ToList();
            }

            if (selectedCards.Count == 0) break;

            selectedCardSet.Add(selectedCards[0]);
            var foodCard = CardUtils.CreateRandomFoodPigCard(player);
            transformations.Add(new CardTransformation(selectedCards[0], foodCard));
        }

        if (transformations.Count > 0)
        {
            await CardCmd.Transform(transformations, player.RunState.Rng.CombatCardGeneration);
        }
    }
}
