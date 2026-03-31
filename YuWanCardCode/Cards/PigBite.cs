using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigBite : YuWanCardModel
{
    public PigBite() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy)
    {
        WithPower<PoisonPower>(4);
        WithPower<WeakPower>(2);
        WithPower<VulnerablePower>(2);
        WithPower<YouArePigPower>(1);
        WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<PoisonPower>()));
        WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<WeakPower>()));
        WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<VulnerablePower>()));
        WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<YouArePigPower>()));
    }

    public override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
        DynamicVars["PoisonPower"].UpgradeValueBy(3);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var targetCreature = cardPlay.Target;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await PowerCmd.Apply<PoisonPower>(targetCreature, DynamicVars["PoisonPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<WeakPower>(targetCreature, DynamicVars["WeakPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<VulnerablePower>(targetCreature, DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<YouArePigPower>(targetCreature, DynamicVars["YouArePigPower"].BaseValue, Owner.Creature, this);
    }
}
