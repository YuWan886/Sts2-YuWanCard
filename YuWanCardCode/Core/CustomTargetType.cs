using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using YuWanCard.Monsters;

namespace YuWanCard.Core;

public static class CustomTargetType
{
    private static readonly DynamicEnumValueMinter<TargetType> TargetTypeMinter = new();

    public static TargetType Everyone { get; } = Mint("everyone");

    public static TargetType Anyone { get; } = Mint("anyone");

    public static TargetType AnyFriendly { get; } = Mint("any_friendly");

    public static TargetType AnyPigMinion { get; } = Mint("any_pig_minion");

    public static bool IsYuWanCustom(TargetType type)
    {
        return type == Everyone
               || type == Anyone
               || type == AnyFriendly
               || type == AnyPigMinion
               || CustomTargetTypeRegistry.IsYuWanCustom(type);
    }

    public static bool IsCustomSingleTargetType(TargetType type)
    {
        return type == Anyone
               || type == AnyFriendly
               || type == AnyPigMinion
               || CustomTargetTypeRegistry.IsCustomSingleTargetType(type);
    }

    public static bool IsCustomMultiTargetType(TargetType type)
    {
        return type == Everyone
               || CustomTargetTypeRegistry.IsCustomMultiTargetType(type);
    }

    public static bool IsAnyFriendlyTarget(Creature? target)
    {
        if (target is not { IsAlive: true })
        {
            return false;
        }

        if (IsAnyPigMinionTarget(target))
        {
            return true;
        }

        return target.IsPlayer && !target.IsPet;
    }

    public static bool IsAnyPigMinionTarget(Creature? target)
    {
        return target is { IsAlive: true, IsPet: true } && target.Monster is PigMinion;
    }

    private static TargetType Mint(string localStem)
    {
        var id = $"{MainFile.ModId.ToUpperInvariant()}_TARGETTYPE_{localStem.ToUpperInvariant()}";
        return TargetTypeMinter.Mint(id);
    }
}
