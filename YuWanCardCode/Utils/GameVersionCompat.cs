using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace YuWanCard.Utils;

public static class GameVersionCompat
{
    #region Version Constants

    public static readonly Version MainBranchVersion = new(0, 99, 1);
    public static readonly Version BetaBranchVersion = new(0, 102, 0);

    #endregion

    #region Version Detection

    private static Version? _cachedVersion;
    private static bool _versionCached;
    private static bool _initialized;

    public static Version? GameVersion
    {
        get
        {
            if (_versionCached)
            {
                return _cachedVersion;
            }

            _versionCached = true;
            var releaseInfo = ReleaseInfoManager.Instance?.ReleaseInfo;
            if (releaseInfo == null || string.IsNullOrEmpty(releaseInfo.Version))
            {
                MainFile.Logger.Warn("GameVersionCompat: Could not get game version from ReleaseInfoManager");
                return null;
            }

            var versionString = releaseInfo.Version;
            if (versionString.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                versionString = versionString[1..];
            }

            if (Version.TryParse(versionString, out var version))
            {
                _cachedVersion = version;
                MainFile.Logger.Info($"GameVersionCompat: Detected game version: {version}");
                return version;
            }

            MainFile.Logger.Warn($"GameVersionCompat: Failed to parse game version: {releaseInfo.Version}");
            return null;
        }
    }

    public static bool IsVersionAtLeast(Version minVersion)
    {
        var currentVersion = GameVersion;
        if (currentVersion == null)
        {
            return false;
        }
        return currentVersion >= minVersion;
    }

    public static bool IsVersionBelow(Version maxVersion)
    {
        var currentVersion = GameVersion;
        if (currentVersion == null)
        {
            return false;
        }
        return currentVersion < maxVersion;
    }

    public static bool IsMainBranch => !IsVersionAtLeast(BetaBranchVersion);
    public static bool IsBetaBranch => IsVersionAtLeast(BetaBranchVersion);

    public static string BranchName => IsBetaBranch ? "beta" : "main";

    #endregion

    #region API Capability Detection

    private static bool? _hasModifyEnergyGain;
    private static bool? _hasVfxDuration;
    private static MethodInfo? _talkCmdPlayMethod;
    private static ConstructorInfo? _mapPointTypeCountsNewCtor;
    private static ConstructorInfo? _mapPointTypeCountsOldCtor;
    private static Type? _vfxDurationType;

    public static bool HasModifyEnergyGainHook
    {
        get
        {
            if (_hasModifyEnergyGain.HasValue)
            {
                return _hasModifyEnergyGain.Value;
            }

            var method = typeof(AbstractModel).GetMethod("ModifyEnergyGain", [typeof(Player), typeof(decimal)]);
            _hasModifyEnergyGain = method != null && method.IsVirtual;
            MainFile.Logger.Info($"GameVersionCompat: ModifyEnergyGain hook available: {_hasModifyEnergyGain}");
            return _hasModifyEnergyGain.Value;
        }
    }

    public static bool HasVfxDurationEnum
    {
        get
        {
            if (_hasVfxDuration.HasValue)
            {
                return _hasVfxDuration.Value;
            }

            _vfxDurationType = typeof(VfxColor).Assembly.GetType("MegaCrit.Sts2.Core.Nodes.Vfx.VfxDuration");
            _hasVfxDuration = _vfxDurationType != null;
            MainFile.Logger.Info($"GameVersionCompat: VfxDuration enum available: {_hasVfxDuration}");
            return _hasVfxDuration.Value;
        }
    }

    private static MethodInfo? TalkCmdPlayMethod
    {
        get
        {
            if (_talkCmdPlayMethod != null)
            {
                return _talkCmdPlayMethod;
            }

            var methods = typeof(TalkCmd).GetMethods().Where(m => m.Name == "Play").ToList();
            _talkCmdPlayMethod = methods.FirstOrDefault();

            if (_talkCmdPlayMethod != null)
            {
                var parms = _talkCmdPlayMethod.GetParameters();
                MainFile.Logger.Info($"GameVersionCompat: TalkCmd.Play signature: {string.Join(", ", parms.Select(p => $"{p.ParameterType.Name} {p.Name}"))}");
            }

            return _talkCmdPlayMethod;
        }
    }

    public static ConstructorInfo? MapPointTypeCountsNewConstructor
    {
        get
        {
            if (_mapPointTypeCountsNewCtor != null || _initialized)
            {
                return _mapPointTypeCountsNewCtor;
            }

            EnsureMapPointTypeCountsConstructorsChecked();
            return _mapPointTypeCountsNewCtor;
        }
    }

    public static ConstructorInfo? MapPointTypeCountsOldConstructor
    {
        get
        {
            if (_mapPointTypeCountsOldCtor != null || _initialized)
            {
                return _mapPointTypeCountsOldCtor;
            }

            EnsureMapPointTypeCountsConstructorsChecked();
            return _mapPointTypeCountsOldCtor;
        }
    }

    private static void EnsureMapPointTypeCountsConstructorsChecked()
    {
        if (_initialized)
        {
            return;
        }

        _mapPointTypeCountsNewCtor = typeof(MapPointTypeCounts).GetConstructor([typeof(int), typeof(int)]);
        _mapPointTypeCountsOldCtor = typeof(MapPointTypeCounts).GetConstructor([typeof(Rng)]);

        MainFile.Logger.Info($"GameVersionCompat: MapPointTypeCounts constructors - New(int, int): {_mapPointTypeCountsNewCtor != null}, Old(Rng): {_mapPointTypeCountsOldCtor != null}");
    }

    #endregion

    #region Initialization

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        MainFile.Logger.Info($"GameVersionCompat: Initializing for branch: {BranchName}");
        MainFile.Logger.Info($"GameVersionCompat: Game version: {GameVersion}");
        MainFile.Logger.Info($"GameVersionCompat: HasModifyEnergyGainHook: {HasModifyEnergyGainHook}");
        MainFile.Logger.Info($"GameVersionCompat: HasVfxDurationEnum: {HasVfxDurationEnum}");

        EnsureMapPointTypeCountsConstructorsChecked();
    }

    #endregion

    #region TalkCmd.Play Unified API

    public static NSpeechBubbleVfx? TalkCmdPlay(LocString line, Creature speaker, VfxColor vfxColor = VfxColor.Red, double duration = -1.0)
    {
        if (speaker == null || speaker.IsDead)
        {
            return null;
        }

        var method = TalkCmdPlayMethod;
        if (method == null)
        {
            MainFile.Logger.Warn("GameVersionCompat: TalkCmd.Play method not available");
            return null;
        }

        try
        {
            var parms = method.GetParameters();

            if (IsBetaBranch && HasVfxDurationEnum)
            {
                return TalkCmdPlayBeta(line, speaker, vfxColor, duration, method, parms);
            }
            else
            {
                return TalkCmdPlayMain(line, speaker, vfxColor, duration, method, parms);
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"GameVersionCompat: TalkCmdPlay failed: {ex.Message}");
            return null;
        }
    }

    private static NSpeechBubbleVfx? TalkCmdPlayBeta(LocString line, Creature speaker, VfxColor vfxColor, double duration, MethodInfo method, ParameterInfo[] parms)
    {
        if (parms.Length == 4)
        {
            var durationValue = CreateVfxDuration(duration);
            return (NSpeechBubbleVfx?)method.Invoke(null, [line, speaker, vfxColor, durationValue]);
        }
        else if (parms.Length == 3 && parms[2].ParameterType == typeof(VfxColor))
        {
            return (NSpeechBubbleVfx?)method.Invoke(null, [line, speaker, vfxColor]);
        }

        return TalkCmdPlayMain(line, speaker, vfxColor, duration, method, parms);
    }

    private static NSpeechBubbleVfx? TalkCmdPlayMain(LocString line, Creature speaker, VfxColor vfxColor, double duration, MethodInfo method, ParameterInfo[] parms)
    {
        if (parms.Length == 4)
        {
            return (NSpeechBubbleVfx?)method.Invoke(null, [line, speaker, duration, vfxColor]);
        }
        else if (parms.Length == 3)
        {
            if (parms[2].ParameterType == typeof(VfxColor))
            {
                return (NSpeechBubbleVfx?)method.Invoke(null, [line, speaker, vfxColor]);
            }
            else if (parms[2].ParameterType == typeof(double))
            {
                return (NSpeechBubbleVfx?)method.Invoke(null, [line, speaker, duration]);
            }
        }
        else if (parms.Length == 2)
        {
            return (NSpeechBubbleVfx?)method.Invoke(null, [line, speaker]);
        }

        MainFile.Logger.Warn($"GameVersionCompat: Unknown TalkCmd.Play signature with {parms.Length} parameters");
        return null;
    }

    private static object? CreateVfxDuration(double duration)
    {
        if (!HasVfxDurationEnum || _vfxDurationType == null)
        {
            return null;
        }

        if (duration < 0)
        {
            var customField = _vfxDurationType.GetField("Custom");
            return customField?.GetValue(null);
        }

        var standardField = _vfxDurationType.GetField("Standard");
        return standardField?.GetValue(null);
    }

    #endregion

    #region MapPointTypeCounts Unified API

    public static MapPointTypeCounts CreateMapPointTypeCounts(Rng rng, int? unknownCount = null, int? restCount = null)
    {
        if (IsBetaBranch && unknownCount.HasValue && restCount.HasValue)
        {
            var ctor = MapPointTypeCountsNewConstructor;
            if (ctor != null)
            {
                return (MapPointTypeCounts)ctor.Invoke([unknownCount.Value, restCount.Value]);
            }
        }

        var oldCtor = MapPointTypeCountsOldConstructor;
        if (oldCtor != null)
        {
            return (MapPointTypeCounts)oldCtor.Invoke([rng]);
        }

        throw new InvalidOperationException("No suitable MapPointTypeCounts constructor found");
    }

    public static bool TrySetNumOfElites(MapPointTypeCounts instance, int newEliteCount)
    {
        if (instance == null)
        {
            return false;
        }

        var propertyInfo = typeof(MapPointTypeCounts).GetProperty(nameof(MapPointTypeCounts.NumOfElites));
        if (propertyInfo != null && propertyInfo.CanWrite)
        {
            propertyInfo.SetValue(instance, newEliteCount);
            MainFile.Logger.Info($"GameVersionCompat: Set NumOfElites to {newEliteCount}");
            return true;
        }

        var backingField = typeof(MapPointTypeCounts).GetField("<NumOfElites>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        if (backingField != null)
        {
            backingField.SetValue(instance, newEliteCount);
            MainFile.Logger.Info($"GameVersionCompat: Set NumOfElites to {newEliteCount} (via backing field)");
            return true;
        }

        MainFile.Logger.Warn("GameVersionCompat: Could not modify NumOfElites - no writable property or backing field found");
        return false;
    }

    #endregion

    #region Energy Gain Unified API

    public static decimal ModifyEnergyGainIfAvailable(Player player, decimal amount)
    {
        if (!HasModifyEnergyGainHook)
        {
            return amount;
        }

        return amount;
    }

    public static bool ShouldUseAfterEnergyReset => !HasModifyEnergyGainHook;

    #endregion
}
