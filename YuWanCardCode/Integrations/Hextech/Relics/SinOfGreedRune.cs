using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Hextech;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

public sealed class SinOfGreedRune : HextechPigRuneBase
{
    private GoldModificationGuard? _goldGuard;

    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Gold;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(1m),
        new DynamicVar("GoldThreshold", 25m),
        new DynamicVar("StrengthCap", 5m),
        new DamageVar(3m, ValueProp.Unpowered)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    private GoldModificationGuard GoldGuard => _goldGuard ??= new GoldModificationGuard(
        () => Owner,
        amount => amount,
        async _ =>
        {
            if (Owner?.Creature?.CombatState == null)
            {
                return;
            }

            var enemies = Owner.Creature.CombatState.HittableEnemies;
            if (enemies.Count == 0)
            {
                return;
            }

            Creature? target = Owner.RunState.Rng.Niche.NextItem(enemies);
            if (target == null)
            {
                return;
            }

            Flash();
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), target, DynamicVars.Damage.BaseValue, ValueProp.Unpowered, Owner.Creature);
        });

    public override Task BeforeCombatStart()
    {
        if (Owner == null)
        {
            return Task.CompletedTask;
        }

        int bonus = Math.Min(
            DynamicVars["StrengthCap"].IntValue,
            (int)Math.Floor(Owner.Gold / DynamicVars["GoldThreshold"].BaseValue));
        if (bonus <= 0)
        {
            return Task.CompletedTask;
        }

        Flash();
        return PowerCmd.Apply<StrengthPower>(Owner.Creature, bonus * DynamicVars.Strength.BaseValue, Owner.Creature, null);
    }

    public override bool ShouldGainGold(decimal amount, Player player)
    {
        return GoldGuard.ShouldGainGold(amount, player);
    }

    public override async Task AfterGoldGained(Player player)
    {
        await GoldGuard.AfterGoldGained(player);
    }
}
