using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class LotsOfMods : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.SingleplayerOnly;

    public LotsOfMods() : base(
        baseCost: 2,
        type: CardType.Attack,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy)
    {
        // 最终伤害 = CalculationBase(6) + ExtraDamage(3/升级后4) × 已加载的 mod 数量。
        WithCalculatedDamage(
            ValueProp.Move,
            multiplierCalc: static (_, _) => ModManager.GetLoadedMods().Count(),
            baseVal: 6,
            extraVal: 3,
            extraUpgrade: 1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        await DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
}
