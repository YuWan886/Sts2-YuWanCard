using MegaCrit.Sts2.Core.Entities.Cards;

namespace YuWanCard.Core;

public static class CustomTargetType
{
    private static readonly DynamicEnumValueMinter<TargetType> TargetTypeMinter = new();

    public static TargetType Everyone { get; } = Mint("everyone");

    public static TargetType Anyone { get; } = Mint("anyone");

    public static TargetType AnyPigMinion { get; } = Mint("any_pig_minion");

    public static bool IsYuWanCustom(TargetType type)
    {
        return type == Everyone
               || type == Anyone
               || type == AnyPigMinion
               || CustomTargetTypeRegistry.IsYuWanCustom(type);
    }

    public static bool IsCustomSingleTargetType(TargetType type)
    {
        return type == Anyone
               || type == AnyPigMinion
               || CustomTargetTypeRegistry.IsCustomSingleTargetType(type);
    }

    public static bool IsCustomMultiTargetType(TargetType type)
    {
        return type == Everyone
               || CustomTargetTypeRegistry.IsCustomMultiTargetType(type);
    }

    private static TargetType Mint(string localStem)
    {
        var id = $"{MainFile.ModId.ToUpperInvariant()}_TARGETTYPE_{localStem.ToUpperInvariant()}";
        return TargetTypeMinter.Mint(id);
    }
}
