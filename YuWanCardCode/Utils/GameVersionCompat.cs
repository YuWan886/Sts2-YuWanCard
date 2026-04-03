using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace YuWanCard.Utils;

public static class GameVersionCompat
{
    private static readonly Version Version0_102_0 = new(0, 102, 0);

    private static Version? _cachedVersion;
    private static bool _versionCached;

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

    public static bool IsVersion099x => IsVersionBelow(Version0_102_0);
    public static bool IsVersion102OrLater => IsVersionAtLeast(Version0_102_0);

    #region TalkCmd.Play Compatibility

    private static readonly Type? VfxDurationType = typeof(VfxColor).Assembly.GetType("MegaCrit.Sts2.Core.Nodes.Vfx.VfxDuration");
    private static readonly object? VfxDurationCustomValue = VfxDurationType?.GetField("Custom")?.GetValue(null);
    private static readonly MethodInfo? TalkCmdPlayMethod = FindTalkCmdPlayMethod();

    private static MethodInfo? FindTalkCmdPlayMethod()
    {
        var methods = typeof(TalkCmd).GetMethods().Where(m => m.Name == "Play").ToList();
        MainFile.Logger.Info($"GameVersionCompat: Found {methods.Count} TalkCmd.Play overloads");
        foreach (var m in methods)
        {
            var parms = m.GetParameters();
            MainFile.Logger.Info($"GameVersionCompat: Play method signature: {string.Join(", ", parms.Select(p => $"{p.ParameterType.Name} {p.Name}"))}");
        }

        var twoParamMethod = typeof(TalkCmd).GetMethod("Play", [typeof(LocString), typeof(Creature)]);
        if (twoParamMethod != null)
        {
            MainFile.Logger.Info("GameVersionCompat: Found 2-param Play method (legacy)");
            return twoParamMethod;
        }

        var anyMethod = methods.FirstOrDefault();
        if (anyMethod != null)
        {
            MainFile.Logger.Info($"GameVersionCompat: Using first available Play method with {anyMethod.GetParameters().Length} params");
        }
        return anyMethod;
    }

    public static void TalkCmdPlay(LocString line, Creature speaker)
    {
        if (TalkCmdPlayMethod == null)
        {
            MainFile.Logger.Warn("GameVersionCompat: TalkCmd.Play method not available, skipping talk line");
            return;
        }

        var parms = TalkCmdPlayMethod.GetParameters();
        if (parms.Length == 2)
        {
            TalkCmdPlayMethod.Invoke(null, [line, speaker]);
        }
        else if (parms.Length == 3)
        {
            TalkCmdPlayMethod.Invoke(null, [line, speaker, VfxColor.Red]);
        }
        else if (parms.Length == 4)
        {
            var paramTypes = parms.Select(p => p.ParameterType).ToArray();

            if (IsVersion099x)
            {
                TalkCmdPlayMethod.Invoke(null, [line, speaker, 3.0, VfxColor.Red]);
            }
            else if (paramTypes[2] == typeof(VfxColor) && VfxDurationCustomValue != null)
            {
                TalkCmdPlayMethod.Invoke(null, [line, speaker, VfxColor.Red, VfxDurationCustomValue]);
            }
            else if (paramTypes[2] == typeof(double) && paramTypes[3] == typeof(VfxColor))
            {
                TalkCmdPlayMethod.Invoke(null, [line, speaker, 3.0, VfxColor.Red]);
            }
            else
            {
                MainFile.Logger.Warn($"GameVersionCompat: Unknown 4-param signature: {string.Join(", ", paramTypes.Select(t => t.Name))}");
            }
        }
        else
        {
            MainFile.Logger.Warn($"GameVersionCompat: Unexpected TalkCmd.Play parameter count: {parms.Length}");
        }
    }

    #endregion

    #region MapPointTypeCounts Compatibility

    private static ConstructorInfo? _mapPointTypeCountsNewConstructor;
    private static ConstructorInfo? _mapPointTypeCountsOldConstructor;
    private static bool _mapPointTypeCountsConstructorsChecked;

    public static ConstructorInfo? MapPointTypeCountsNewConstructor
    {
        get
        {
            EnsureMapPointTypeCountsConstructorsChecked();
            return _mapPointTypeCountsNewConstructor;
        }
    }

    public static ConstructorInfo? MapPointTypeCountsOldConstructor
    {
        get
        {
            EnsureMapPointTypeCountsConstructorsChecked();
            return _mapPointTypeCountsOldConstructor;
        }
    }

    private static void EnsureMapPointTypeCountsConstructorsChecked()
    {
        if (_mapPointTypeCountsConstructorsChecked)
        {
            return;
        }

        _mapPointTypeCountsConstructorsChecked = true;
        _mapPointTypeCountsNewConstructor = typeof(MapPointTypeCounts).GetConstructor([typeof(int), typeof(int)]);
        _mapPointTypeCountsOldConstructor = typeof(MapPointTypeCounts).GetConstructor([typeof(Rng)]);

        MainFile.Logger.Info($"GameVersionCompat: MapPointTypeCounts constructors - New(int, int): {_mapPointTypeCountsNewConstructor != null}, Old(Rng): {_mapPointTypeCountsOldConstructor != null}");
    }

    public static void TrySetNumOfElites(MapPointTypeCounts instance, int newEliteCount, int eliteBonus, int loopCount)
    {
        var propertyInfo = typeof(MapPointTypeCounts).GetProperty(nameof(MapPointTypeCounts.NumOfElites));
        if (propertyInfo != null && propertyInfo.CanWrite)
        {
            propertyInfo.SetValue(instance, newEliteCount);
            MainFile.Logger.Info($"GameVersionCompat: Increased elite count from {newEliteCount - eliteBonus} to {newEliteCount} (Loop {loopCount}, Bonus +{eliteBonus})");
        }
        else
        {
            var backingField = typeof(MapPointTypeCounts).GetField("<NumOfElites>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            if (backingField != null)
            {
                backingField.SetValue(instance, newEliteCount);
                MainFile.Logger.Info($"GameVersionCompat: Increased elite count from {newEliteCount - eliteBonus} to {newEliteCount} (Loop {loopCount}, Bonus +{eliteBonus}) [via backing field]");
            }
            else
            {
                MainFile.Logger.Warn("GameVersionCompat: Could not modify NumOfElites - no writable property or backing field found");
            }
        }
    }

    #endregion
}
