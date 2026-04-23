using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class DimensionSlash : YuWanCardModel
{
    public DimensionSlash() : base(
        baseCost: 0,
        type: CardType.Attack,
        rarity: CardRarity.Ancient,
        target: TargetType.AnyEnemy)
    {
        WithDamage(15);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var target = cardPlay.Target;
        var combatState = Owner.Creature.CombatState;
        
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var debuffsToApply = new List<(PowerModel Power, int Amount)>();
        
        foreach (var power in target.Powers)
        {
            if (power is PowerModel powerModel && powerModel.Type == PowerType.Debuff)
            {
                var doubledAmount = powerModel.Amount * 2;
                await PowerCmd.Apply(powerModel, target, doubledAmount, Owner.Creature, this);
                debuffsToApply.Add((powerModel, doubledAmount));
            }
        }

        var otherEnemies = combatState?.Enemies.Where(e => e != target && e.IsAlive).ToList() ?? new List<Creature>();
        
        foreach (var enemy in otherEnemies)
        {
            foreach (var (power, amount) in debuffsToApply)
            {
                await PowerCmd.Apply(power, enemy, amount, Owner.Creature, this);
            }
        }

        if (!TestMode.IsOn)
        {
            VfxUtils.PlayCentered("res://YuWanCard/scenes/vfx/vfx_glass_shatter.tscn");
            AudioUtils.Play("res://YuWanCard/sounds/vfx/glass_shatter.mp3");
        }
    }
}