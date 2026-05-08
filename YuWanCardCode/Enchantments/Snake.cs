using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Enchantments;

public sealed class Snake : YuWanEnchantmentModel
{
    public override bool ShowAmount => true;
    public override bool IsStackable => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (Card == null || cardPlay == null) return;

        var snakebiteModel = ModelDb.Card<Snakebite>();
        if (snakebiteModel == null) return;

        for (int i = 0; i < Amount; i++)
        {
            var snakebiteCard = Card.CombatState!.CreateCard(snakebiteModel, Card.Owner);
            await CardPileCmd.AddGeneratedCardToCombat(snakebiteCard, PileType.Hand, cardPlay.Card.Owner);
        }
    }
}
