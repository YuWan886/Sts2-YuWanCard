using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class TiaoJiao : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public TiaoJiao() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyAlly)
    {
        WithDamage(4);
        WithPower<TrainYouWellPower>(3);
        WithTip(new TooltipSource(_ => HoverTipFactory.FromPower<StrengthPower>()));
    }

    public override void OnUpgrade()
    {
        DynamicVars["TrainYouWellPower"].UpgradeValueBy(2m);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .Execute(choiceContext);
        
        await PowerCmd.Apply<TrainYouWellPower>(cardPlay.Target, DynamicVars["TrainYouWellPower"].IntValue, Owner.Creature, this);
    }
}
