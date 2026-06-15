using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigBully : YuWanCardModel
{
    private const int BaseDamage = 9;
    private const int TeammateDamageBonus = 3;
    private const int TeammateDamageBonusUpgraded = 4;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public PigBully() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy)
    {
        // 最终伤害 = BaseDamage + 每名队友的加成 × 存活队友数量。
        WithCalculatedDamage(
            ValueProp.Move,
            multiplierCalc: static (card, _) => (card as PigBully)?.GetTeammateCount() ?? 0,
            baseVal: BaseDamage,
            extraVal: TeammateDamageBonus,
            extraUpgrade: TeammateDamageBonusUpgraded - TeammateDamageBonus);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            await DamageCmd.Attack(DynamicVars.CalculatedDamage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    public int GetTeammateCount()
    {
        if (CombatState == null) return 0;
        var teammates = CombatState.GetTeammatesOf(Owner.Creature);
        return teammates.Count(t => t.IsAlive);
    }
}
