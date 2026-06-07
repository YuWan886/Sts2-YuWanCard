using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Relics.Balatro;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public sealed class CollectorJoker : BalatroJokerRelicModel
{
    private const int CardsPerEnergy = 5;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);

        if (Owner == null || player != Owner)
        {
            return;
        }

        int multiplier = EffectiveCount();
        if (multiplier <= 0)
        {
            return;
        }

        int rareCount = player.Deck.Cards.Count(card =>
            card.Rarity is CardRarity.Rare or CardRarity.Ancient);
        int energy = rareCount / CardsPerEnergy * multiplier;
        if (energy > 0)
        {
            await PlayerCmd.GainEnergy(energy, player);
        }
    }

}
