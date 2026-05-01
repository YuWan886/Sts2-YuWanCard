using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace YuWanCard.Core.Utils;

/// <summary>
/// Compatibility layer for CreatureCmd.SetMaxHp / SetMaxAndCurrentHp.
/// On live game builds (e.g. v0.103.2) these methods may not exist yet.
///
/// IMPORTANT: The async method bodies must NEVER reference the missing API methods
/// directly — the .NET JIT compiler fails during async state machine compilation
/// before any try/catch can execute. All calls go through delegates resolved
/// at static init via reflection.
/// </summary>
public static class CreatureCompat
{
    // Resolved at runtime from the sts2 assembly — avoids compile-time reference to potentially missing type
    private static readonly Type? _creatureCmdType =
        Type.GetType("MegaCrit.Sts2.Core.Commands.CreatureCmd, sts2");

    // Resolved at static init — SetMaxHp(func or null), SetMaxHpInternal(fallback), SetCurrentHp(safe)
    private static readonly Func<Creature, decimal, Task>? _setMaxHpFunc;
    private static readonly Func<Creature, decimal, Task>? _setMaxAndCurrentHpFunc;
    private static readonly Action<Creature, decimal>? _setMaxHpInternal;
    private static readonly Func<Creature, decimal, Task>? _setCurrentHpFunc;

    static CreatureCompat()
    {
        // Resolve Creature.SetMaxHpInternal for fallback path
        var internalMethod = typeof(Creature).GetMethod("SetMaxHpInternal",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(decimal) }, null);
        if (internalMethod != null)
        {
            _setMaxHpInternal = (creature, amount) =>
                internalMethod.Invoke(creature, new object[] { amount });
        }

        // Probe CreatureCmd.SetMaxHp — resolved via reflection so JIT never sees a direct call
        _setMaxHpFunc = ResolveStaticAsyncMethod("SetMaxHp", typeof(Creature), typeof(decimal));

        // Probe CreatureCmd.SetMaxAndCurrentHp
        _setMaxAndCurrentHpFunc = ResolveStaticAsyncMethod("SetMaxAndCurrentHp", typeof(Creature), typeof(decimal));

        // Probe CreatureCmd.SetCurrentHp — used by SetMaxAndCurrentHp fallback
        _setCurrentHpFunc = ResolveStaticAsyncMethod("SetCurrentHp", typeof(Creature), typeof(decimal));
    }

    private static Func<Creature, decimal, Task>? ResolveStaticAsyncMethod(
        string name, params Type[] paramTypes)
    {
        if (_creatureCmdType == null) return null;
        var method = _creatureCmdType.GetMethod(name,
            BindingFlags.Public | BindingFlags.Static, null, paramTypes, null);
        if (method == null) return null;
        return (creature, amount) =>
        {
            var task = (Task)method.Invoke(null, new object[] { creature, amount })!;
            return task;
        };
    }

    /// <summary>Sets max HP. Falls back to Creature.SetMaxHpInternal if the API is missing.</summary>
    public static Task SetMaxHp(Creature creature, decimal amount)
    {
        if (_setMaxHpFunc != null)
            return _setMaxHpFunc(creature, amount);

        SetMaxHpFallback(creature, amount);
        return Task.CompletedTask;
    }

    /// <summary>Sets both max and current HP. Falls back to internal methods if the API is missing.</summary>
    public static async Task SetMaxAndCurrentHp(Creature creature, decimal amount)
    {
        if (_setMaxAndCurrentHpFunc != null)
        {
            await _setMaxAndCurrentHpFunc(creature, amount);
            return;
        }

        SetMaxHpFallback(creature, amount);
        if (_setCurrentHpFunc != null)
            await _setCurrentHpFunc(creature, amount);
    }

    private static void SetMaxHpFallback(Creature creature, decimal amount)
    {
        amount = Math.Max(0m, amount);
        if (_setMaxHpInternal != null)
        {
            _setMaxHpInternal(creature, amount);
        }
        else
        {
            MainFile.Logger.Error(
                "[CreatureCompat] Both CreatureCmd.SetMaxHp and Creature.SetMaxHpInternal are unavailable.");
        }
    }
}
