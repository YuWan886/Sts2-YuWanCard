using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Interop;

namespace YuWanCard.Core.Extensions;

public static class CardModelTargetingExtensions
{
    public static List<Creature> GetSelectableTargets(this CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);

        var state = card.CombatState;
        if (state == null)
            return [];

        return card.TargetType switch
        {
            TargetType.AnyEnemy or TargetType.AllEnemies or TargetType.RandomEnemy
                => state.HittableEnemies.ToList(),
            TargetType.AnyAlly or TargetType.AllAllies
                => state.Allies.Where(c => c != null && c.IsAlive).ToList(),
            TargetType.AnyPlayer
                => state.Players.Where(p => p?.Creature is { IsAlive: true }).Select(p => p.Creature).ToList(),
            TargetType.None => [],
            TargetType.Self => [card.Owner.Creature],
            _ => GetCustomSelectableTargets(card, state)
        };
    }

    public static Creature? PickRandomTarget(this CardModel card)
    {
        var candidates = card.GetSelectableTargets();
        if (candidates.Count == 0)
            return null;

        return card.Owner.RunState.Rng.CombatTargets.NextItem(candidates);
    }

    private static List<Creature> GetCustomSelectableTargets(CardModel card, ICombatState state)
    {
        if (CustomTargetType.IsCustomSingleTargetType(card.TargetType))
        {
            return state.Creatures
                .Where(c =>
                    CustomTargetTypeRegistry.TryIsAllowedSingleTarget(card.TargetType, c, out var allowed) && allowed)
                .ToList();
        }

        if (CustomTargetType.IsCustomMultiTargetType(card.TargetType))
        {
            return state.Creatures
                .Where(c =>
                    CustomTargetTypeRegistry.TryShouldIncludeMultiTarget(card.TargetType, c, out var include) && include)
                .ToList();
        }

        return ExternalCardTargetingCompat.TryGetSelectableTargets(card, state, out var externalTargets)
            ? externalTargets
            : [];
    }
}

internal static class ExternalCardTargetingCompat
{
    private static readonly string[] CandidateModIds = ["STS2-RitsuLib", "BaseLib"];
    private static readonly Lock SyncRoot = new();

    private static IReadOnlyList<ExternalTargetingBridge>? _cachedBridges;

    internal static bool TryGetSelectableTargets(CardModel card, ICombatState state, out List<Creature> targets)
    {
        foreach (var bridge in GetBridges())
        {
            if (bridge.TryGetSelectableTargets(card, state, out targets))
            {
                return true;
            }
        }

        targets = [];
        return false;
    }

    private static IReadOnlyList<ExternalTargetingBridge> GetBridges()
    {
        lock (SyncRoot)
        {
            if (_cachedBridges is { Count: > 0 })
            {
                return _cachedBridges;
            }

            return _cachedBridges = BuildBridges();
        }
    }

    private static IReadOnlyList<ExternalTargetingBridge> BuildBridges()
    {
        List<ExternalTargetingBridge> bridges = [];

        foreach (var modId in CandidateModIds)
        {
            if (!ModCompat.TryGetAssembly(modId, out var assembly) || assembly == null)
            {
                continue;
            }

            if (TryCreateBridge(modId, assembly) is { } bridge)
            {
                bridges.Add(bridge);
            }
        }

        return bridges;
    }

    private static ExternalTargetingBridge? TryCreateBridge(string modId, Assembly assembly)
    {
        Type? customTargetType = null;
        Type? targetingExtensionsType = null;

        foreach (var type in GetTypesSafely(assembly))
        {
            if (customTargetType == null
                && type.Name == "CustomTargetType"
                && FindStaticMethod(type, "IsCustomSingleTargetType", typeof(TargetType)) != null
                && FindStaticMethod(type, "IsCustomMultiTargetType", typeof(TargetType)) != null)
            {
                customTargetType = type;
            }

            if (targetingExtensionsType == null
                && type.Name == "CardModelTargetingExtensions"
                && FindStaticMethod(type, "GetTargets", typeof(CardModel), typeof(Creature)) != null)
            {
                targetingExtensionsType = type;
            }

            if (customTargetType != null && targetingExtensionsType != null)
            {
                break;
            }
        }

        if (customTargetType == null || targetingExtensionsType == null)
        {
            return null;
        }

        var isSingleMethod = FindStaticMethod(customTargetType, "IsCustomSingleTargetType", typeof(TargetType));
        var isMultiMethod = FindStaticMethod(customTargetType, "IsCustomMultiTargetType", typeof(TargetType));
        var getTargetsMethod = FindStaticMethod(targetingExtensionsType, "GetTargets", typeof(CardModel), typeof(Creature));
        if (isSingleMethod == null || isMultiMethod == null || getTargetsMethod == null)
        {
            return null;
        }

        return new ExternalTargetingBridge(modId, isSingleMethod, isMultiMethod, getTargetsMethod);
    }

    private static IEnumerable<Type> GetTypesSafely(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }

    private static MethodInfo? FindStaticMethod(Type type, string name, params Type[] parameterTypes)
    {
        return type
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(method => method.Name == name && ParametersMatch(method, parameterTypes));
    }

    private static bool ParametersMatch(MethodInfo method, Type[] parameterTypes)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != parameterTypes.Length)
        {
            return false;
        }

        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType != parameterTypes[i])
            {
                return false;
            }
        }

        return true;
    }

    private sealed class ExternalTargetingBridge(
        string modId,
        MethodInfo isSingleMethod,
        MethodInfo isMultiMethod,
        MethodInfo getTargetsMethod)
    {
        internal bool TryGetSelectableTargets(CardModel card, ICombatState state, out List<Creature> targets)
        {
            if (InvokeBool(isSingleMethod, card.TargetType))
            {
                targets = state.Creatures
                    .Where(card.IsValidTarget)
                    .ToList();
                return true;
            }

            if (!InvokeBool(isMultiMethod, card.TargetType))
            {
                targets = [];
                return false;
            }

            targets = InvokeTargets(card);
            return true;
        }

        private static bool InvokeBool(MethodInfo method, TargetType targetType)
        {
            return method.Invoke(null, [targetType]) is bool value && value;
        }

        private List<Creature> InvokeTargets(CardModel card)
        {
            if (getTargetsMethod.Invoke(null, [card, null]) is IEnumerable<Creature> targets)
            {
                return targets.ToList();
            }

            MainFile.Logger.Warn($"ExternalCardTargetingCompat[{modId}] returned a non-creature target list.");
            return [];
        }
    }
}
