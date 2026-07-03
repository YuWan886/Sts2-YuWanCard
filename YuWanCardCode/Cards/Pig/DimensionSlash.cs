using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.TestSupport;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class DimensionSlash : YuWanCardModel
{
    private static readonly HashSet<Type> s_safeDebuffWhitelist =
    [
        typeof(VulnerablePower),
        typeof(WeakPower),
        typeof(FrailPower),
    ];

    private readonly record struct DebuffCopy(PowerModel Canonical, decimal AmountToDouble, decimal AmountToCopy);

    public DimensionSlash() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Rare,
        target: TargetType.AnyEnemy)
    {
        WithDamage(15);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var target = cardPlay.Target;
        var combatState = Owner.Creature.CombatState;
        
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, null)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // Snapshot first so we can double the target without mutating the enumeration,
        // then give every other living enemy a fresh debuff instance with the doubled result.
        var debuffsToCopy = target.Powers
            .Where(power => power.IsVisible && power.Type == PowerType.Debuff)
            .Select(TryCaptureDebuffCopy)
            .OfType<DebuffCopy>()
            .ToList();

        foreach (var debuff in debuffsToCopy)
        {
            await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), debuff.Canonical.ToMutable(), target, debuff.AmountToDouble, Owner.Creature, this);
        }

        var otherEnemies = combatState?.Enemies.Where(e => e != target && e.IsAlive).ToList() ?? new List<Creature>();

        foreach (var enemy in otherEnemies)
        {
            foreach (var debuff in debuffsToCopy)
            {
                await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), debuff.Canonical.ToMutable(), enemy, debuff.AmountToCopy, Owner.Creature, this);
            }
        }

        if (!TestMode.IsOn)
        {
            VfxUtils.PlayCentered("res://YuWanCard/scenes/vfx/vfx_glass_shatter.tscn");
            AudioUtils.Play("res://YuWanCard/sounds/vfx/glass_shatter.mp3");
        }
    }

    private static DebuffCopy? TryCaptureDebuffCopy(PowerModel power)
    {
        // Base game stack debuffs like Vulnerable are safe to re-create from canonical data,
        // but our generic analyzer can conservatively reject them because of harmless dealer access.
        if (!IsCopySafe(power))
        {
            return null;
        }

        var canonical = ModelDb.GetByIdOrNull<PowerModel>(power.Id);
        if (canonical == null)
        {
            return null;
        }

        try
        {
            _ = canonical.ToMutable();
            decimal baseAmount = power.Amount <= 0 ? 1 : power.Amount;
            decimal amountToDouble = baseAmount;
            decimal amountToCopy = power.StackType == PowerStackType.Counter ? baseAmount * 2 : baseAmount;
            return new DebuffCopy(canonical, amountToDouble, amountToCopy);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[DimensionSlash] 跳过无法复制的减益 {power.Id}：{ex.Message}");
            return null;
        }
    }

    private static bool IsCopySafe(PowerModel power)
    {
        var powerType = power.GetType();
        return s_safeDebuffWhitelist.Any(t => t.IsAssignableFrom(powerType))
               || PowerSafetyUtils.IsSafePower(power);
    }
}
