using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
        rarity: CardRarity.Rare,
        target: TargetType.AnyAlly)
    {
        WithPower<RegenPower>(3);
        WithVar("HealPercentage", 10);
        WithKeywords(CardKeyword.Exhaust);
    }

    public decimal HealPercentage => DynamicVars["HealPercentage"].IntValue / 100m;

    protected override void OnUpgrade()
    {
        DynamicVars["RegenPower"].UpgradeValueBy(1);   
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var targetCreature = cardPlay.Target;
        int healAmount = (int)(targetCreature.MaxHp * HealPercentage);

        await CreatureCmd.Heal(targetCreature, healAmount);
        await PowerCmd.Apply<RegenPower>(targetCreature, DynamicVars["RegenPower"].IntValue, Owner.Creature, this);
    }
}
