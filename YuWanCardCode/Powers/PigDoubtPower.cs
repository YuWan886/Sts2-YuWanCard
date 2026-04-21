using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class PigDoubtPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("PigDoubtPower", 1m)];

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
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

                var randomPower = GetRandomPower();
                if (randomPower != null)
                {
                    var mutablePower = randomPower.ToMutable();
                    if (mutablePower != null)
                    {
                        await PowerCmd.Apply(mutablePower, Owner, 1, Owner, null);
                    }
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

        var filteredPowers = ModelDb.AllPowers
            .Where(p => !p.IsInstanced && IsSafePower(p) && IsValidPower(p) && p.Type == PowerType.Buff)
            .ToList();

        if (filteredPowers.Count == 0) return null;

        return rng.Niche.NextItem(filteredPowers);
    }

    private bool IsValidPower(PowerModel power)
    {
        if (power.Id == null)
        {
            return false;
        }

        try
        {
            var mutable = power.ToMutable();
            if (mutable == null)
            {
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[PigDoubtPower] 能力 {power.Id} ToMutable() 失败：{ex.Message}");
            return false;
        }
    }

    private bool IsSafePower(PowerModel power)
    {
        if (power is YuWanPowerModel)
        {
            return false;
        }

        return PowerSafetyUtils.IsSafePower(power);
    }
}
