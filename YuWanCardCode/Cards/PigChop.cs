using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigChop : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public PigChop() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Token,
        target: TargetType.Self)
    {
        WithPower<RegenPower>(3);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["RegenPower"].UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<RegenPower>(Owner.Creature, DynamicVars["RegenPower"].BaseValue, Owner.Creature, this);
    }
}
