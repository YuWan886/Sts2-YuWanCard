using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Enchantments;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class CrystalPig : YuWanCardModel
{
    public CrystalPig() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithVar("Count", 1, 1);
        WithTip(typeof(Glam));
        WithTip(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var glam = ModelDb.Enchantment<Glam>();
        var selectableCards = PileType.Draw.GetPile(Owner).Cards
            .Where(glam.CanEnchant)
            .ToList();

        if (selectableCards.Count == 0)
        {
            return;
        }

        int count = Math.Min(DynamicVars["Count"].IntValue, selectableCards.Count);
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, count);
        var selectedCards = await CardSelectCmd.FromSimpleGrid(choiceContext, selectableCards, Owner, prefs);

        foreach (var selectedCard in selectedCards)
        {
            CardCmd.Enchant<Glam>(selectedCard, 1);
            CardCmd.ApplyKeyword(selectedCard, CardKeyword.Exhaust);
        }
    }
}
