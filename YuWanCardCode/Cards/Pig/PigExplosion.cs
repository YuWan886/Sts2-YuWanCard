using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YuWanCard.Characters;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigExplosion : YuWanCardModel
{
    public PigExplosion() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.Self)
    {
        WithTip(typeof(PigExplosionPower));
        WithVars(
            new DynamicVar("Turns", 2m),
            new DynamicVar("PigExplosionDamage", 20m)
        );
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var power = await PowerCmd.Apply<PigExplosionPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["Turns"].BaseValue, Owner.Creature, this);
        if (power != null)
        {
            power.SetDamage(DynamicVars["PigExplosionDamage"].BaseValue);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PigExplosionDamage"].UpgradeValueBy(8m);
    }
}
