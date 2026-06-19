using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.RelicPools;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class SmallDeck : YuWanRelicModel
{
    private const int MaxRemoveCount = 5;
    private static readonly LocString SelectionPrompt = new("relics", "YUWANCARD-SMALL_DECK.selectionPrompt");

    public override RelicRarity Rarity => RelicRarity.Rare;

    public SmallDeck() : base(true)
    {
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner == null)
        {
            return;
        }

        var removableCards = PileType.Deck.GetPile(Owner).Cards
            .Where(c => c.IsRemovable)
            .ToList();

        if (removableCards.Count == 0)
        {
            return;
        }

        int maxCount = Math.Min(MaxRemoveCount, removableCards.Count);

        var prefs = new CardSelectorPrefs(SelectionPrompt, 0, maxCount)
        {
            Cancelable = true
        };

        var selected = (await CardSelectCmd.FromDeckForRemoval(Owner, prefs)).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        Flash();
        await CardPileCmd.RemoveFromDeck(selected);
    }
}
