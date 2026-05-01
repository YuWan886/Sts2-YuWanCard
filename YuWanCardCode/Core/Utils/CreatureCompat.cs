using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace YuWanCard.Core.Utils;

/// <summary>
/// Compatibility layer for CreatureCmd.SetMaxHp / SetMaxAndCurrentHp.
/// On live game builds (e.g. v0.103.2) these methods may not exist yet.
/// Falls back to calling Creature.SetMaxHpInternal directly when the official API is missing.
/// </summary>
public static class CreatureCompat
{
    private static readonly Action<Creature, decimal>? _setMaxHpInternal;

    static CreatureCompat()
    {
        var method = typeof(Creature).GetMethod("SetMaxHpInternal",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(decimal) }, null);
        if (method != null)
        {
            _setMaxHpInternal = (creature, amount) =>
                method.Invoke(creature, new object[] { amount });
        }
    }

    public static async Task SetMaxHp(Creature creature, decimal amount)
    {
        try
        {
            await CreatureCmd.SetMaxHp(creature, amount);
        }
        catch (MissingMethodException)
        {
            SetMaxHpFallback(creature, amount);
        }
    }

    public static async Task SetMaxAndCurrentHp(Creature creature, decimal amount)
    {
        try
        {
            await CreatureCmd.SetMaxAndCurrentHp(creature, amount);
        }
        catch (MissingMethodException)
        {
            SetMaxHpFallback(creature, amount);
            await CreatureCmd.SetCurrentHp(creature, amount);
        }
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
                "[CreatureCompat] CreatureCmd.SetMaxHp missing and Creature.SetMaxHpInternal not found via reflection.");
        }
    }
}
