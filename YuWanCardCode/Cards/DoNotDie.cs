using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class DoNotDie : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public DoNotDie() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyPlayer)
    {
        WithPower<RegenPower>(3);
        WithKeywords(CardKeyword.Exhaust);
        WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<RegenPower>()));
    }

    protected override void OnUpgrade()
    {
        DynamicVars["RegenPower"].UpgradeValueBy(1);   
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var targetCreature = cardPlay.Target;
        int healAmount = (int)(targetCreature.MaxHp * 0.1m);

        await CreatureCmd.Heal(targetCreature, healAmount);
        await PowerCmd.Apply<RegenPower>(targetCreature, DynamicVars["RegenPower"].IntValue, Owner.Creature, this);
    }
}
