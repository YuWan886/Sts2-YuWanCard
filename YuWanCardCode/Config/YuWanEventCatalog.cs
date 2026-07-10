using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Extensions;

namespace YuWanCard.Config;

internal readonly record struct YuWanEventDefinition(
    string Key,
    Type EventType,
    string IdEntry);

internal static class YuWanEventCatalog
{
    public const int ButtonsPerRow = 3;

    public static readonly IReadOnlyList<YuWanEventDefinition> Events = BuildEvents();

    private static readonly Dictionary<Type, YuWanEventDefinition> DefinitionsByType =
        Events.ToDictionary(static definition => definition.EventType);

    private static readonly Dictionary<string, YuWanEventDefinition> DefinitionsByKey =
        Events.ToDictionary(static definition => definition.Key, StringComparer.Ordinal);

    public static bool TryGetDefinition(Type eventType, out YuWanEventDefinition definition)
        => DefinitionsByType.TryGetValue(eventType, out definition);

    public static bool TryGetDefinition(string key, out YuWanEventDefinition definition)
        => DefinitionsByKey.TryGetValue(key, out definition);

    public static string GetDisplayTitle(YuWanEventDefinition definition)
    {
        string title = new LocString("events", $"{definition.IdEntry}.title").GetRawText();
        return string.IsNullOrWhiteSpace(title) ? definition.EventType.Name : title;
    }

    public static string GetInitialDescription(YuWanEventDefinition definition)
    {
        string description = new LocString("events", $"{definition.IdEntry}.pages.INITIAL.description").GetRawText();
        return string.IsNullOrWhiteSpace(description) ? GetDisplayTitle(definition) : description;
    }

    private static IReadOnlyList<YuWanEventDefinition> BuildEvents()
    {
        return typeof(YuWanEventCatalog).Assembly
            .GetTypes()
            .Where(static type =>
                !type.IsAbstract
                && type.IsAssignableTo(typeof(YuWanEventModel)))
            .Select(Create)
            .OrderBy(static definition => definition.Key, StringComparer.Ordinal)
            .ToArray();
    }

    private static YuWanEventDefinition Create(Type eventType)
    {
        string idEntry = ModelDb.GetId(eventType).Entry;
        return new YuWanEventDefinition(
            idEntry.RemovePrefix().ToLowerInvariant(),
            eventType,
            idEntry);
    }
}
