using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Timeline;
using YuWanCard.Timeline.Epochs;
using YuWanCard.Timeline.Stories;

namespace YuWanCard.Timeline;

internal static class PigTimelineRegistry
{
    internal const string StoryId = "Pig";
    internal const string StoryKey = "PIG";
    internal const string DefaultPortraitPath = "res://YuWanCard/images/characters/char_select_pig.png";
    internal const string EpochPortraitDirectory = "res://YuWanCard/images/timeline/epoch_portraits";

    private static readonly object SyncRoot = new();

    private static readonly string[] PigEpochIds =
    [
        Pig1Epoch.EpochId,
        Pig2Epoch.EpochId,
        Pig3Epoch.EpochId,
        Pig4Epoch.EpochId,
        Pig5Epoch.EpochId,
        Pig6Epoch.EpochId,
        Pig7Epoch.EpochId
    ];

    private static readonly Type[] PigEpochTypes =
    [
        typeof(Pig1Epoch),
        typeof(Pig2Epoch),
        typeof(Pig3Epoch),
        typeof(Pig4Epoch),
        typeof(Pig5Epoch),
        typeof(Pig6Epoch),
        typeof(Pig7Epoch)
    ];

    private static readonly Type[] PigStoryTypes =
    [
        typeof(PigStory)
    ];

    private static bool _registered;

    private static readonly Dictionary<string, Type> EpochTypeDictionary =
        AccessTools.StaticFieldRefAccess<Dictionary<string, Type>>(
            typeof(EpochModel),
            "_epochTypeDictionary");

    private static readonly Dictionary<Type, string> EpochIdDictionary =
        AccessTools.StaticFieldRefAccess<Dictionary<Type, string>>(
            typeof(EpochModel),
            "_typeToIdDictionary");

    private static readonly Dictionary<string, Type> StoryTypeDictionary =
        AccessTools.StaticFieldRefAccess<Dictionary<string, Type>>(
            typeof(StoryModel),
            "_storyTypeDictionary");

    private static readonly FieldInfo? AllEpochIdsField =
        AccessTools.Field(typeof(EpochModel), "_allEpochIds");

    public static void EnsureRegistered()
    {
        if (_registered)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_registered)
            {
                return;
            }

            RegisterEpochs();
            RegisterStories();
            SyncAllEpochIds();
            _registered = true;
        }
    }

    public static void SyncAllEpochIds()
    {
        var existing = EpochModel.AllEpochIds
            .Where(id => !PigEpochIds.Contains(id, StringComparer.Ordinal))
            .ToList();

        foreach (string pigEpochId in PigEpochIds)
        {
            if (!existing.Contains(pigEpochId, StringComparer.Ordinal))
            {
                existing.Add(pigEpochId);
            }
        }

        AllEpochIdsField?.SetValue(null, existing);
    }

    private static void RegisterEpochs()
    {
        foreach (Type epochType in PigEpochTypes)
        {
            var epoch = (EpochModel?)Activator.CreateInstance(epochType);
            if (epoch == null)
            {
                throw new InvalidOperationException($"Failed to instantiate pig epoch type {epochType.FullName}.");
            }

            EpochTypeDictionary[epoch.Id] = epochType;
            EpochIdDictionary[epochType] = epoch.Id;
        }
    }

    private static void RegisterStories()
    {
        foreach (Type storyType in PigStoryTypes)
        {
            StoryTypeDictionary[StoryKey] = storyType;
        }
    }

    internal static string BuildEpochPortraitPath(string epochId)
        => $"{EpochPortraitDirectory}/{epochId.ToLowerInvariant()}.png";

    internal static bool IsPigEpochId(string epochId)
        => PigEpochIds.Contains(epochId, StringComparer.Ordinal);

    internal static string GetResolvedEpochPortraitPath(string epochId)
    {
        string portraitPath = BuildEpochPortraitPath(epochId);
        return ResourceLoader.Exists(portraitPath) ? portraitPath : DefaultPortraitPath;
    }
}

public abstract class PigEpochBase : EpochModel
{
    public sealed override string? StoryId => PigTimelineRegistry.StoryId;

    internal virtual string EpochPortraitPath => PigTimelineRegistry.BuildEpochPortraitPath(Id);

    internal virtual string CustomPackedPortraitPath => EpochPortraitPath;

    internal virtual string CustomResolvedPortraitPath => PigTimelineRegistry.GetResolvedEpochPortraitPath(Id);

    internal virtual bool UsesPlaceholderPortrait => false;
}
