using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Powers;

public class PigDoubtPower : YuWanPowerModel
{
    private static readonly Dictionary<Type, bool> SafetyCache = new();
    private static readonly object _lock = new();

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner.Side)
        {
            Flash();
            int powerCount = Amount;

            for (int i = 0; i < powerCount; i++)
            {
                if (CombatManager.Instance.IsEnding)
                {
                    break;
                }

                var randomPower = GetRandomPower();
                if (randomPower != null)
                {
                    await PowerCmd.Apply(randomPower.ToMutable(), Owner, 1, Owner, null);
                }

                if (await CombatManager.Instance.CheckWinCondition())
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
            .Where(p => !p.IsInstanced && IsSafePower(p))
            .ToList();

        if (filteredPowers.Count == 0) return null;

        return rng.Niche.NextItem(filteredPowers);
    }

    private bool IsSafePower(PowerModel power)
    {
        var powerType = power.GetType();

        if (power is YuWanPowerModel)
        {
            return false;
        }

        try
        {
            if (power.ShouldStopCombatFromEnding())
            {
                return false;
            }
        }
        catch (NullReferenceException)
        {
            return false;
        }

        lock (_lock)
        {
            if (SafetyCache.TryGetValue(powerType, out var isSafe))
            {
                return isSafe;
            }

            bool result = AnalyzePowerSafety(powerType);
            SafetyCache[powerType] = result;
            
            return result;
        }
    }

    private static bool AnalyzePowerSafety(Type powerType)
    {
        try
        {
            var methods = powerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
            foreach (var method in methods)
            {
                if (method.IsStatic || method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                {
                    continue;
                }

                if (MethodHasMonsterCast(method))
                {
                    return false;
                }

                var asyncAttr = method.GetCustomAttribute<System.Runtime.CompilerServices.AsyncStateMachineAttribute>();
                if (asyncAttr != null)
                {
                    var stateMachineType = asyncAttr.StateMachineType;
                    if (stateMachineType != null)
                    {
                        var moveNextMethod = stateMachineType.GetMethod("MoveNext", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (moveNextMethod != null && MethodHasMonsterCast(moveNextMethod))
                        {
                            return false;
                        }
                    }
                }
            }

            var properties = powerType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var prop in properties)
            {
                var getter = prop.GetGetMethod(true);
                if (getter != null && !getter.IsStatic)
                {
                    if (MethodHasMonsterCast(getter))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[PigDoubtPower] 分析能力失败：{powerType.Name}, 错误：{ex.Message}");
            return true;
        }
    }

    private static bool MethodHasMonsterCast(MethodInfo method)
    {
        try
        {
            var methodBody = method.GetMethodBody();
            if (methodBody == null)
            {
                return false;
            }

            var ilBytes = methodBody.GetILAsByteArray();
            if (ilBytes == null)
            {
                return false;
            }

            var module = method.Module;

            for (int i = 0; i < ilBytes.Length - 4; i++)
            {
                byte opCode = ilBytes[i];

                if (opCode == 0x74)
                {
                    if (i + 4 < ilBytes.Length)
                    {
                        int token = BitConverter.ToInt32(ilBytes, i + 1);
                        try
                        {
                            var resolvedType = module.ResolveType(token);
                            if (resolvedType != null &&
                                resolvedType != typeof(MonsterModel) &&
                                typeof(MonsterModel).IsAssignableFrom(resolvedType))
                            {
                                return true;
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[PigDoubtPower] IL 分析失败：{method.Name}, 错误：{ex.Message}");
            return false;
        }
    }
}
