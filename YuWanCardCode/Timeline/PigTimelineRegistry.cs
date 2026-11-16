using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
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

    /// <summary>
    /// Preferred timeline slot for each pig epoch. Row 0 is the bottom of an era column and row 4 is the
    /// top, and two epochs must never own the same (era, row) pair: the timeline rebuilds slot state by
    /// matching that pair against saved progress, so overlapping epochs share each other's reveal state
    /// and the vanilla epoch becomes impossible to distinguish from the pig one.
    /// These slots must stay free in every supported game version. The Fool (愚者) epochs reflowed the
    /// vanilla rows in 0.113 (Invitation2:3, Flourish2:2, Flourish3:4 and Invitation7:2 are now taken),
    /// so the values below are the slots that are still free in both 0.107.1 and 0.113.x.
    /// <see cref="ResolvePlacements"/> relocates an entry automatically if a future update takes its slot.
    /// </summary>
    private static readonly (string EpochId, EpochEra Era, int Position)[] DeclaredPlacements =
    [
        (Pig1Epoch.EpochId, EpochEra.Invitation2, 4),
        (Pig2Epoch.EpochId, EpochEra.Flourish0, 3),
        (Pig3Epoch.EpochId, EpochEra.Flourish2, 3),
        (Pig4Epoch.EpochId, EpochEra.Blight0, 1),
        (Pig5Epoch.EpochId, EpochEra.Blight1, 4),
        (Pig6Epoch.EpochId, EpochEra.Blight2, 2),
        (Pig7Epoch.EpochId, EpochEra.Invitation7, 3)
    ];

    /// <summary>
    /// Highest row the timeline uses. Vanilla epochs only occupy 0-4, and a column lays out one row per
    /// slot, so staying inside that range keeps pig epochs aligned with the vanilla grid.
    /// </summary>
    internal const int MaxEraPosition = 4;

    private static readonly Dictionary<string, (EpochEra Era, int Position)> ResolvedPlacements = new();

    private static bool _placementsResolved;

    private static bool _registered;

    private static readonly Dictionary<string, Type> EpochTypeDictionary =
        AccessTools.StaticFieldRefAccess<Dictionary<string, Type>>(
            typeof(EpochModel),
            "_epochTypeDictionary");

    private static readonly Dictionary<Type, string> EpochIdDictionary =
        AccessTools.StaticFieldRefAccess<Dictionary<Type, string>>(
            typeof(EpochModel),
            "_typeToIdDictionary");

    private static readonly List<Type> AllEpochTypes =
        AccessTools.StaticFieldRefAccess<List<Type>>(
            typeof(EpochModel),
            "_allEpochs");

    private static readonly Dictionary<string, Type> StoryTypeDictionary =
        AccessTools.StaticFieldRefAccess<Dictionary<string, Type>>(
            typeof(StoryModel),
            "_storyTypeDictionary");

    private static readonly FieldInfo? AllEpochIdsField =
        AccessTools.Field(typeof(EpochModel), "_allEpochIds");

    private static readonly FieldInfo? EpochIdsHashSetField =
        AccessTools.Field(typeof(EpochModel), "_epochIdsHashSet");

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

            ResolvePlacements();
            RegisterEpochs();
            RegisterStories();
            SyncAllEpochIds();
            _registered = true;
        }
    }

    public static void SyncAllEpochIds()
    {
        AllEpochIdsField?.SetValue(null, null);

        // EpochIdsHashSet caches the id set the first time EpochModel.IsValid() runs, and save loading
        // prunes every epoch whose id is not in it. Clearing only the list would let progress saved with
        // pig epochs be treated as invalid (and removed) once the hash set had been built before the
        // pig epochs were registered, so rebuild both caches.
        EpochIdsHashSetField?.SetValue(null, null);
    }

    /// <summary>
    /// Timeline slot this epoch reports to the game. Resolved once at registration so the value stays
    /// stable for the whole session.
    /// </summary>
    internal static (EpochEra Era, int Position) GetPlacement(string epochId)
    {
        // Placement resolution runs before the pig epoch types are registered, so an Era read during
        // registration must not call back into EnsureRegistered() and recurse.
        if (!_placementsResolved)
        {
            EnsureRegistered();
        }

        if (ResolvedPlacements.TryGetValue(epochId, out (EpochEra Era, int Position) placement))
        {
            return placement;
        }

        foreach ((string declaredId, EpochEra era, int position) in DeclaredPlacements)
        {
            if (declaredId == epochId)
            {
                return (era, position);
            }
        }

        return (EpochEra.Seeds0, 0);
    }

    /// <summary>
    /// Claims the declared slot of every pig epoch, moving any epoch whose slot was taken by another
    /// epoch (a newer game version adding or reflowing vanilla epochs) to the closest free one.
    /// Runs before pig epoch types join <see cref="EpochModel.AllEpochs"/>, so it only sees the epochs
    /// the game itself registered.
    /// </summary>
    private static void ResolvePlacements()
    {
        if (_placementsResolved)
        {
            return;
        }

        ResolvedPlacements.Clear();

        HashSet<(EpochEra Era, int Position)> occupied = [];

        foreach (Type epochType in AllEpochTypes)
        {
            if (PigEpochTypes.Contains(epochType))
            {
                continue;
            }

            EpochModel? epoch = null;
            try
            {
                epoch = Activator.CreateInstance(epochType) as EpochModel;
            }
            catch (Exception exception)
            {
                Log.Warn($"[YuWanCard] Could not read timeline slot of {epochType.FullName}: {exception.Message}");
            }

            if (epoch != null)
            {
                occupied.Add((epoch.Era, epoch.EraPosition));
            }
        }

        foreach ((string epochId, EpochEra era, int position) in DeclaredPlacements)
        {
            (EpochEra Era, int Position) placement = (era, position);

            if (occupied.Contains(placement))
            {
                placement = FindFreePlacement(era, occupied);
                Log.Warn(
                    $"[YuWanCard] Timeline slot {era}:{position} of epoch {epochId} is already owned by another epoch; " +
                    $"moved it to {placement.Era}:{placement.Position}. Update DeclaredPlacements to silence this warning.");
            }

            occupied.Add(placement);
            ResolvedPlacements[epochId] = placement;
        }

        _placementsResolved = true;
    }

    private static (EpochEra Era, int Position) FindFreePlacement(
        EpochEra preferredEra,
        HashSet<(EpochEra Era, int Position)> occupied)
    {
        for (int position = 0; position <= MaxEraPosition; position++)
        {
            if (!occupied.Contains((preferredEra, position)))
            {
                return (preferredEra, position);
            }
        }

        foreach (EpochEra era in ErasByProximity(preferredEra))
        {
            for (int position = 0; position <= MaxEraPosition; position++)
            {
                if (!occupied.Contains((era, position)))
                {
                    return (era, position);
                }
            }
        }

        return (preferredEra, 0);
    }

    private static IEnumerable<EpochEra> ErasByProximity(EpochEra era)
    {
        return Enum.GetValues<EpochEra>()
            .Where(candidate => candidate != era)
            .OrderBy(candidate => Math.Abs((long)candidate - (long)era))
            .ThenBy(candidate => (int)candidate);
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

            if (!AllEpochTypes.Contains(epochType))
            {
                AllEpochTypes.Add(epochType);
            }
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

    public override EpochEra Era => PigTimelineRegistry.GetPlacement(Id).Era;

    public override int EraPosition => PigTimelineRegistry.GetPlacement(Id).Position;

    internal virtual string EpochPortraitPath => PigTimelineRegistry.BuildEpochPortraitPath(Id);

    internal virtual string CustomPackedPortraitPath => EpochPortraitPath;

    internal virtual string CustomResolvedPortraitPath => PigTimelineRegistry.GetResolvedEpochPortraitPath(Id);

    internal virtual bool UsesPlaceholderPortrait => false;
}
