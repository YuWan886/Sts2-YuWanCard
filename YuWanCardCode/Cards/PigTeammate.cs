using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigTeammate : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public PigTeammate() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyAlly)
    {
        WithPower<BufferPower>(1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        WithKeywords(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var targetPlayer = cardPlay.Target.Player;
        if (targetPlayer == null || targetPlayer == Owner) return;

        var handCards = PileType.Hand.GetPile(targetPlayer).Cards.ToList();
        foreach (var card in handCards)
        {
            await CardPileCmd.RemoveFromCombat(card);

            var newCard = CombatState!.CreateCard(card.CanonicalInstance, Owner);
            if (card.CurrentUpgradeLevel > 0)
            {
                for (int i = 0; i < card.CurrentUpgradeLevel; i++)
                {
                    CardCmd.Upgrade(newCard);
                }
            }
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner);
        }

        var energyToTake = targetPlayer.PlayerCombatState?.Energy ?? 0;
        await PlayerCmd.SetEnergy(0, targetPlayer);
        await PlayerCmd.GainEnergy(energyToTake, Owner);

        await PowerCmd.Apply<BufferPower>(choiceContext, targetPlayer.Creature, 1, Owner.Creature, this);

        await CardPileCmd.Add(this, PileType.Exhaust);
    }
}
