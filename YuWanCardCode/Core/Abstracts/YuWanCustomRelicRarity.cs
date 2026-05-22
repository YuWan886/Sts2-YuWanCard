using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace YuWanCard.Core.Abstracts;

public sealed class YuWanCustomRelicRarity(
    string id,
    string headerLocalizationTable,
    string headerLocalizationKey,
    string? displayLocalizationTable = null,
    string? displayLocalizationKey = null,
    RelicRarity visualRarity = RelicRarity.None,
    int sortOrder = 0)
{
    public string Id { get; } = id;

    public string HeaderLocalizationTable { get; } = headerLocalizationTable;

    public string HeaderLocalizationKey { get; } = headerLocalizationKey;

    public string DisplayLocalizationTable { get; } = displayLocalizationTable ?? headerLocalizationTable;

    public string DisplayLocalizationKey { get; } = displayLocalizationKey ?? headerLocalizationKey;

    public RelicRarity VisualRarity { get; } = visualRarity;

    public int SortOrder { get; } = sortOrder;

    public LocString CreateHeader() => new(HeaderLocalizationTable, HeaderLocalizationKey);

    public LocString CreateDisplayLabel() => new(DisplayLocalizationTable, DisplayLocalizationKey);
}
