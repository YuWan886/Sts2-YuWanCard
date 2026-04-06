using System.Text.Json;
using System.Text.Json.Serialization;

namespace YuWanCard.Utils;

public static class UpdateChecker
{
    private const string GitHubApiUrl = "https://api.github.com/repos/YuWan886/Sts2-YuWanCard/releases/latest";
    private const string ModId = "YuWanCard";
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 1000;

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static bool _hasCheckedThisSession = false;
    private static readonly object _lock = new();
    private static string? _cachedVersion;

    public static string LatestVersion { get; private set; } = string.Empty;
    public static string ReleaseUrl { get; private set; } = string.Empty;

    public static string CurrentVersion => GetCurrentVersion();

    static UpdateChecker()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "YuWanCard-UpdateChecker");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    }

    private static string GetCurrentVersion()
    {
        if (_cachedVersion != null)
        {
            return _cachedVersion;
        }

        try
        {
            var modManagerType = Type.GetType("MegaCrit.Sts2.Core.Modding.ModManager, sts2");
            if (modManagerType != null)
            {
                var modsProperty = modManagerType.GetProperty("Mods");
                if (modsProperty != null)
                {
                    var mods = modsProperty.GetValue(null) as System.Collections.IEnumerable;
                    if (mods != null)
                    {
                        foreach (var mod in mods)
                        {
                            var manifestField = mod.GetType().GetField("manifest");
                            if (manifestField != null)
                            {
                                var manifest = manifestField.GetValue(mod);
                                if (manifest != null)
                                {
                                    var idField = manifest.GetType().GetField("id");
                                    var versionField = manifest.GetType().GetField("version");
                                    
                                    if (idField != null && versionField != null)
                                    {
                                        var id = idField.GetValue(manifest) as string;
                                        if (id == ModId)
                                        {
                                            var version = versionField.GetValue(manifest) as string;
                                            if (!string.IsNullOrEmpty(version))
                                            {
                                                if (!version.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    version = "v" + version;
                                                }
                                                _cachedVersion = version;
                                                return version;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Debug($"Failed to get version from ModManager: {ex.Message}");
        }

        try
        {
            string manifestPath = Path.Combine(GetModDirectory(), "YuWanCard.json");
            if (File.Exists(manifestPath))
            {
                var jsonContent = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<ModManifestData>(jsonContent);
                if (manifest != null && !string.IsNullOrEmpty(manifest.Version))
                {
                    string version = manifest.Version;
                    if (!version.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                    {
                        version = "v" + version;
                    }
                    _cachedVersion = version;
                    return version;
                }
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Debug($"Failed to read version from manifest: {ex.Message}");
        }

        return "v0.0.0";
    }

    private static string GetModDirectory()
    {
        try
        {
            string executablePath = Godot.OS.GetExecutablePath();
            string? directoryName = Path.GetDirectoryName(executablePath);
            if (!string.IsNullOrEmpty(directoryName))
            {
                return Path.Combine(directoryName, "mods", ModId);
            }
        }
        catch { }
        
        return string.Empty;
    }

    public static async Task<UpdateCheckResult> CheckForUpdateAsync()
    {
        lock (_lock)
        {
            if (_hasCheckedThisSession)
            {
                return new UpdateCheckResult { AlreadyChecked = true };
            }
            _hasCheckedThisSession = true;
        }

        MainFile.Logger.Debug("Checking for updates...");

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var response = await _httpClient.GetStringAsync(GitHubApiUrl);
                var releaseInfo = JsonSerializer.Deserialize<GitHubRelease>(response);

                if (releaseInfo == null || string.IsNullOrEmpty(releaseInfo.TagName))
                {
                    MainFile.Logger.Warn("Failed to parse release info");
                    return new UpdateCheckResult { Success = false };
                }

                LatestVersion = releaseInfo.TagName;
                ReleaseUrl = releaseInfo.HtmlUrl ?? "https://github.com/YuWan886/Sts2-YuWanCard";

                string currentVersion = CurrentVersion;
                MainFile.Logger.Debug($"Latest version: {LatestVersion}, Current version: {currentVersion}");

                bool hasUpdate = CompareVersions(LatestVersion, currentVersion) > 0;

                return new UpdateCheckResult
                {
                    Success = true,
                    HasUpdate = hasUpdate,
                    CurrentVersion = currentVersion,
                    LatestVersion = LatestVersion,
                    ReleaseUrl = ReleaseUrl
                };
            }
            catch (HttpRequestException ex)
            {
                MainFile.Logger.Debug($"Network error during update check (attempt {attempt}/{MaxRetries}): {ex.Message}");
                
                if (attempt < MaxRetries)
                {
                    await Task.Delay(RetryDelayMs * attempt);
                }
            }
            catch (TaskCanceledException)
            {
                MainFile.Logger.Debug($"Update check timed out (attempt {attempt}/{MaxRetries})");
                
                if (attempt < MaxRetries)
                {
                    await Task.Delay(RetryDelayMs * attempt);
                }
            }
            catch (Exception ex)
            {
                MainFile.Logger.Debug($"Unexpected error during update check: {ex.Message}");
                return new UpdateCheckResult { Success = false };
            }
        }

        MainFile.Logger.Debug("Update check failed after all retries");
        return new UpdateCheckResult { Success = false };
    }

    private static int CompareVersions(string version1, string version2)
    {
        string NormalizeVersion(string v)
        {
            return v.TrimStart('v', 'V');
        }

        var v1Parts = NormalizeVersion(version1).Split('.');
        var v2Parts = NormalizeVersion(version2).Split('.');

        int maxLength = Math.Max(v1Parts.Length, v2Parts.Length);

        for (int i = 0; i < maxLength; i++)
        {
            int v1Part = i < v1Parts.Length && int.TryParse(v1Parts[i], out int v1) ? v1 : 0;
            int v2Part = i < v2Parts.Length && int.TryParse(v2Parts[i], out int v2) ? v2 : 0;

            if (v1Part != v2Part)
            {
                return v1Part.CompareTo(v2Part);
            }
        }

        return 0;
    }

    public static void ResetCheckState()
    {
        lock (_lock)
        {
            _hasCheckedThisSession = false;
        }
    }

    private class ModManifestData
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }
    }
}

public class UpdateCheckResult
{
    public bool AlreadyChecked { get; set; }
    public bool Success { get; set; }
    public bool HasUpdate { get; set; }
    public string CurrentVersion { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public string ReleaseUrl { get; set; } = string.Empty;
}

internal class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }
}
