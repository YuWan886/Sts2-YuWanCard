using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class YuWanForgot : YuWanCardModel
{
    public YuWanForgot() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithVars(new EnergyVar(2));
        WithCards(2);
        WithKeywords(CardKeyword.Exhaust);
        WithEnergyTip();
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable || Owner == null) return false;
            return !PileType.Hand.GetPile(Owner).Cards.Any(card => card != this);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
        DynamicVars.Energy.UpgradeValueBy(1);
        DynamicVars.Cards.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }
}
