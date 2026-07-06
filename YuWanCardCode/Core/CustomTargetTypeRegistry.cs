using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core;

internal static class CustomTargetTypeRegistry
{
    private static readonly Dictionary<TargetType, Func<CardModel?, Creature, bool>> SingleTargetPredicates = [];
    private static readonly Dictionary<TargetType, Func<Creature, bool>> MultiTargetPredicates = [];

    internal static bool IsYuWanCustom(TargetType type)
    {
        return SingleTargetPredicates.ContainsKey(type) || MultiTargetPredicates.ContainsKey(type);
    }

    internal static bool IsCustomSingleTargetType(TargetType type)
    {
        return SingleTargetPredicates.ContainsKey(type);
    }

    internal static bool IsCustomMultiTargetType(TargetType type)
    {
        return MultiTargetPredicates.ContainsKey(type);
    }

    internal static bool TryIsAllowedSingleTarget(TargetType type, CardModel? card, Creature creature, out bool allowed)
    {
        if (!SingleTargetPredicates.TryGetValue(type, out var predicate))
        {
            allowed = false;
            return false;
        }

        allowed = predicate(card, creature);
        return true;
    }

    internal static bool TryShouldIncludeMultiTarget(TargetType type, Creature creature, out bool include)
    {
        if (!MultiTargetPredicates.TryGetValue(type, out var predicate))
        {
            include = false;
            return false;
        }

        include = predicate(creature);
        return true;
    }

    internal static void RegisterSingleTargetType(TargetType type, Func<CardModel?, Creature, bool> predicate)
    {
        SingleTargetPredicates[type] = predicate;
    }

    internal static void RegisterMultiTargetType(TargetType type, Func<Creature, bool> predicate)
    {
        MultiTargetPredicates[type] = predicate;
    }

    internal static void RegisterBuiltIns()
    {
        SingleTargetPredicates.Clear();
        MultiTargetPredicates.Clear();

        RegisterSingleTargetType(CustomTargetType.Anyone, (_, target) => target is { IsAlive: true, IsPet: false });
        RegisterSingleTargetType(CustomTargetType.AnyFriendly, (_, target) => CustomTargetType.IsAnyFriendlyTarget(target));
        RegisterMultiTargetType(CustomTargetType.Everyone, target => target is { IsAlive: true, IsPet: false });
        RegisterSingleTargetType(CustomTargetType.AnyPigMinion, (_, target) => CustomTargetType.IsAnyPigMinionTarget(target));
        RegisterSingleTargetType(CustomTargetType.AnyYouArePigTarget, (_, target) => CustomTargetType.IsAnyYouArePigTarget(target));
        RegisterSingleTargetType(CustomTargetType.AnyPigPawnTarget, (_, target) => CustomTargetType.IsAnyPigPawnTarget(target));
    }
}
