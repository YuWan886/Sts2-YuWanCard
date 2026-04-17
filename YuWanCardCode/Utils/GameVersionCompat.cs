using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace YuWanCard.Utils;

public static class GameVersionCompat
{
    #region Version Constants

    public static readonly Version CurrentVersion = new(0, 103, 2);

    #endregion

    #region Version Detection

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

    #endregion
}
