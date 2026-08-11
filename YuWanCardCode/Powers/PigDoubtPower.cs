using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class PigDoubtPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("PigDoubtPower", 1m)];

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == Owner.Side)
        {
            Flash();
            int powerCount = Amount;

            for (int i = 0; i < powerCount; i++)
            {
                if (CombatManager.Instance?.IsEnding != false)
                {
                    break;
                }

                var mutablePower = GetRandomPower();
                if (mutablePower != null)
                {
                    await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), mutablePower, Owner, 1, Owner, null);
                }

                if (CombatManager.Instance != null && await CombatManager.Instance.CheckWinCondition())
                {
                    break;
                }
            }
        }
    }

    private PowerModel? GetRandomPower()
    {
        var rng = Owner.Player?.RunState.Rng;
        if (rng == null) return null;

        // ModelDb.AllPowers contains canonical (immutable) models. Some optional mods
        // patch PowerModel.Type and assert mutability, so inspect only mutable clones.
        var filteredPowers = ModelDb.AllPowers
            .Select(TryCreateEligiblePower)
            .Where(p => p != null)
            .Select(p => p!)
            .ToList();

        if (filteredPowers.Count == 0) return null;

        return DeterministicRandomUtils.PickDeterministicBuffPower(filteredPowers, rng.CombatCardSelection);
    }

    private PowerModel? TryCreateEligiblePower(PowerModel canonicalPower)
    {
        if (canonicalPower.Id == null)
        {
            return null;
        }

        try
        {
            var mutablePower = canonicalPower.ToMutable();
            if (mutablePower.InstanceType != PowerInstanceType.None
                || !IsSafePower(mutablePower)
                || mutablePower.Type != PowerType.Buff)
            {
                return null;
            }

            return mutablePower;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[PigDoubtPower] 能力 {canonicalPower.Id} 创建可变副本或筛选失败：{ex.Message}");
            return null;
        }
    }

    private bool IsSafePower(PowerModel power)
    {
        return PowerSafetyUtils.IsSafePower(power);
    }
}
