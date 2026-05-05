using MegaCrit.Sts2.Core.Entities.Cards;

namespace YuWanCard.Core;

public static class CustomTargetType
{
    private static readonly DynamicEnumValueMinter<TargetType> TargetTypeMinter = new();

    public static TargetType Everyone { get; } = Mint("everyone");

    public static TargetType Anyone { get; } = Mint("anyone");

    public static bool IsYuWanCustom(TargetType type)
    {
        return type == Everyone || type == Anyone;
    }

    private static TargetType Mint(string localStem)
    {
        var id = $"{MainFile.ModId.ToUpperInvariant()}_TARGETTYPE_{localStem.ToUpperInvariant()}";
        return TargetTypeMinter.Mint(id);
    }
}
