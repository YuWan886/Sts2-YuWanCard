using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class BullyBigPig : YuWanCardModel
{
    public BullyBigPig() : base(
        baseCost: 3,
        type: CardType.Power,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithPower<HardenedShellPower>(15);
        WithVar("MaxHpGain", 6);
        WithKeyword(CardKeyword.Innate, UpgradeType.Add);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<HardenedShellPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["HardenedShellPower"].BaseValue,
            Owner.Creature,
            this);
        await CreatureCmd.GainMaxHp(Owner.Creature, DynamicVars["MaxHpGain"].IntValue);
    }
}
