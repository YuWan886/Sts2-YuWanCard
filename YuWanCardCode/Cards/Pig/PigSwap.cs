using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigSwap : YuWanCardModel
{
    public PigSwap() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        var playerCombatState = player.PlayerCombatState;
        if (playerCombatState != null)
        {
            var drawPile = PileType.Draw.GetPile(player);
            var discardPile = PileType.Discard.GetPile(player);

            var tempCards = drawPile.Cards.ToList();
            YuWanReflectionHelper.SetPrivateField(drawPile, "_cards", discardPile.Cards.ToList());
            YuWanReflectionHelper.SetPrivateField(discardPile, "_cards", tempCards);

            drawPile.InvokeCardAddFinished();
            drawPile.InvokeCardRemoveFinished();
            drawPile.InvokeContentsChanged();
            discardPile.InvokeCardAddFinished();
            discardPile.InvokeCardRemoveFinished();
            discardPile.InvokeContentsChanged();

            int costPaid = EnergyCost.GetWithModifiers(CostModifiers.All);
            playerCombatState.Energy = playerCombatState.MaxEnergy - (playerCombatState.Energy + costPaid);
        }

        return Task.CompletedTask;
    }
}
