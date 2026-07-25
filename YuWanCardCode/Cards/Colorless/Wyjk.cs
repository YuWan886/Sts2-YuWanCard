using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class Wyjk : YuWanCardModel
{
    public Wyjk() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithEnergy(2, 1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars["Energy"].IntValue, Owner);
        VfxUtils.PlayAtCreatureTop("res://YuWanCard/scenes/vfx/vfx_wyjk.tscn", Owner.Creature);
    }
}
