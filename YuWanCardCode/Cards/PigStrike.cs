using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigStrike : YuWanCardModel
{
    public override CardPoolModel Pool => ModelDb.CardPool<IroncladCardPool>();

    public PigStrike() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Basic,
        target: TargetType.AnyEnemy)
    {
        WithDamage(6);
        WithTags(CardTag.Strike);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }
}
