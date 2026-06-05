using System.Reflection;
using System.Text;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Characters;
using YuWanCard.Modifiers;
using YuWanCard.Relics;

namespace YuWanCard.Utils;

public static class CloudAnalyticsService
{
    private const string ConfigFileName = "posthog.analytics.yaml";
    private const string LocalConfigFileName = "posthog.analytics.local.yaml";
    private const string StateFileName = "posthog.analytics.state.json";
    private const string DefaultHost = "https://us.i.posthog.com";
    private const string CapturePath = "/i/v0/e/";

    private static readonly object SyncRoot = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static readonly System.Net.Http.HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static CloudAnalyticsConfig _config = new();
    private static CloudAnalyticsState _state = new();
    private static ActiveRunTelemetry? _activeRun;
    private static RunState? _pendingNewRunState;
    private static string _localConfigPath = string.Empty;
    private static string _configPath = string.Empty;
    private static string _statePath = string.Empty;
    private static bool _initialized;
    private static bool _sessionRegistered;
    private static bool _attemptedLoadChineseCharacterTable;
    private static LocTable? _cachedChineseCharacterTable;

    static CloudAnalyticsService()
    {
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "YuWanCard-Analytics");
    }

    public static void Initialize()
    {
        lock (SyncRoot)
        {
            if (_initialized)
            {
                return;
            }

            string analyticsDir = EnsureAnalyticsDirectory();
            _localConfigPath = GetLocalConfigPath();
            _configPath = Path.Combine(analyticsDir, ConfigFileName);
            _statePath = Path.Combine(analyticsDir, StateFileName);

            _config = LoadConfig(_localConfigPath, _configPath);
            _state = LoadState(_statePath);
            EnsureStableIds();
            SaveStateUnsafe();

            _initialized = true;
        }

        if (!_config.CanSendAnalytics)
        {
            MainFile.Logger.Info($"Cloud analytics disabled. Configure '{_configPath}' to enable PostHog reporting.");
            return;
        }

        MainFile.Logger.Info($"Cloud analytics configured: {_config.Host}");
        TryRegisterSessionStart();
    }

    public static void OnRunStarted(RunState runState)
    {
        EnsureInitialized();
        MarkPendingNewRun(runState);
        TryRegisterRunStart(runState);
    }

    public static void OnRunObserved(RunState? runState)
    {
        EnsureInitialized();
        TryRegisterRunStart(runState, requirePendingNewRun: true);
    }

    public static void OnRunEnded(RunState? runState, bool isVictory)
    {
        EnsureInitialized();
        if (!CanCollect() || !_config.CaptureRunEvents)
        {
            return;
        }

        TryRegisterRunStart(runState, requirePendingNewRun: true);
        CaptureRunEndState(runState);

        ActiveRunTelemetry? runToReport;
        lock (SyncRoot)
        {
            if (_activeRun == null || _activeRun.HasReportedEnd)
            {
                return;
            }

            _activeRun.HasReportedEnd = true;
            runToReport = _activeRun;
            _activeRun = null;

            if (isVictory)
            {
                _state.TotalRunsWon++;
                if (runToReport.IsPig)
                {
                    _state.TotalPigRunsWon++;
                }
            }
            else
            {
                _state.TotalRunsLost++;
                if (runToReport.IsPig)
                {
                    _state.TotalPigRunsLost++;
                }
            }

            SaveStateUnsafe();
        }

        SendIdentifySnapshot();
        SendEvent(
            "run_ended",
            BuildRunProperties(runToReport, isVictory ? "victory" : "defeat"));
    }

    public static bool ShouldShowConsentPrompt()
    {
        EnsureInitialized();
        lock (SyncRoot)
        {
            return _config.CanSendAnalytics && !_state.AnalyticsConsentDecided;
        }
    }

    public static bool IsCollectionEnabled()
    {
        EnsureInitialized();
        lock (SyncRoot)
        {
            return CanCollectUnsafe();
        }
    }

    public static void SetCollectionEnabled(bool enabled)
    {
        EnsureInitialized();

        lock (SyncRoot)
        {
            _state.AnalyticsConsentDecided = true;
            _state.AnalyticsCollectionEnabled = enabled;
            SaveStateUnsafe();
        }

        if (enabled)
        {
            MainFile.Logger.Info("Cloud analytics enabled by user consent");
            TryRegisterSessionStart();
        }
        else
        {
            MainFile.Logger.Info("Cloud analytics disabled by user consent");
        }
    }

    private static void RegisterSessionStart()
    {
        lock (SyncRoot)
        {
            _state.TotalSessionsStarted++;
            _state.LastSessionStartedUtc = DateTime.UtcNow;
            _state.LastModVersion = UpdateChecker.CurrentVersion;
            SaveStateUnsafe();
        }

        SendIdentifySnapshot();

        if (_config.CaptureLaunchEvents)
        {
            SendEvent("mod_session_started", new Dictionary<string, object?>
            {
                ["session_count"] = _state.TotalSessionsStarted,
                ["mod_version"] = UpdateChecker.CurrentVersion,
                ["os_name"] = OS.GetName(),
                ["godot_platform"] = OS.GetDistributionName(),
                ["timestamp_utc"] = DateTime.UtcNow.ToString("O")
            });
        }
    }

    private static void TryRegisterSessionStart()
    {
        if (!CanCollect())
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_sessionRegistered)
            {
                return;
            }

            _sessionRegistered = true;
        }

        RegisterSessionStart();
    }

    private static void MarkPendingNewRun(RunState runState)
    {
        lock (SyncRoot)
        {
            _pendingNewRunState = runState;
        }
    }

    private static bool TryRegisterRunStart(RunState? runState, bool requirePendingNewRun = false)
    {
        if (runState == null || !CanCollect() || !_config.CaptureRunEvents)
        {
            return false;
        }

        Player? localPlayer = LocalContext.GetMe(runState);
        if (localPlayer?.Character == null)
        {
            return false;
        }

        ActiveRunTelemetry? runToReport = null;
        bool isPig = localPlayer.Character is Pig;

        lock (SyncRoot)
        {
            if (_activeRun != null && ReferenceEquals(_activeRun.SourceRunState, runState))
            {
                return true;
            }

            bool isPendingNewRun = ReferenceEquals(_pendingNewRunState, runState);
            if (requirePendingNewRun && !isPendingNewRun)
            {
                return false;
            }

            runToReport = new ActiveRunTelemetry
            {
                SourceRunState = runState,
                RunId = Guid.NewGuid().ToString("N"),
                CharacterId = localPlayer.Character.Id.Entry,
                CharacterType = localPlayer.Character.GetType().Name,
                CharacterNameZh = ResolveCharacterChineseTitle(localPlayer.Character),
                IsPig = isPig,
                PlayerCount = runState.Players.Count,
                IsMultiplayer = IsMultiplayer(runState),
                AscensionLevel = runState.AscensionLevel,
                InstalledModList = GetInstalledModList(),
                StartedAtUtc = DateTime.UtcNow
            };

            _activeRun = runToReport;
            if (isPendingNewRun)
            {
                _pendingNewRunState = null;
            }
            _state.TotalRunsStarted++;
            if (isPig)
            {
                _state.TotalPigRunsStarted++;
            }
            SaveStateUnsafe();
        }

        SendIdentifySnapshot();
        SendEvent("run_started", BuildRunProperties(runToReport, null));
        return true;
    }

    private static Dictionary<string, object?> BuildRunProperties(ActiveRunTelemetry run, string? result)
    {
        var properties = new Dictionary<string, object?>
        {
            ["run_id"] = run.RunId,
            ["character_id"] = run.CharacterId,
            ["character_type"] = run.CharacterType,
            ["character_name_zh"] = run.CharacterNameZh,
            ["is_pig"] = run.IsPig,
            ["player_count"] = run.PlayerCount,
            ["is_multiplayer"] = run.IsMultiplayer,
            ["ascension_level"] = run.AscensionLevel,
            ["mod_version"] = UpdateChecker.CurrentVersion,
            ["installed_mod_list"] = run.InstalledModList,
            ["run_started_at_utc"] = run.StartedAtUtc.ToString("O")
        };

        if (result != null)
        {
            properties["result"] = result;
            properties["duration_seconds"] = Math.Max(0, (int)(DateTime.UtcNow - run.StartedAtUtc).TotalSeconds);
            properties["has_ring_of_seven_curses"] = run.HasRingOfSevenCursesAtEnd;
            properties["malice_level"] = run.MaliceLevelAtEnd;
        }

        return properties;
    }

    private static void CaptureRunEndState(RunState? runState)
    {
        if (runState == null)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_activeRun == null || _activeRun.HasReportedEnd)
            {
                return;
            }

            Player? trackedPlayer = ResolveTrackedPlayer(runState, _activeRun.CharacterId);
            if (trackedPlayer?.Character == null)
            {
                return;
            }

            _activeRun.HasRingOfSevenCursesAtEnd = trackedPlayer.GetRelic<RingOfSevenCurses>() != null;
            _activeRun.MaliceLevelAtEnd = MaliceModifier.GetMaliceModifier(runState)?.EffectiveMaliceLevel ?? 0;
        }
    }

    private static Player? ResolveTrackedPlayer(RunState runState, string characterId)
    {
        Player? localPlayer = LocalContext.GetMe(runState);
        if (localPlayer?.Character != null)
        {
            return localPlayer;
        }

        return runState.Players.FirstOrDefault(player => player.Character?.Id.Entry == characterId);
    }

    private static string ResolveCharacterChineseTitle(CharacterModel character)
    {
        string locKey = $"{character.Id.Entry}.title";

        try
        {
            LocTable? chineseCharacterTable = GetChineseCharacterTable();
            if (chineseCharacterTable != null)
            {
                string title = chineseCharacterTable.GetRawText(locKey);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Cloud analytics failed to resolve zh character title for '{character.Id.Entry}': {ex.Message}");
        }

        string fallbackTitle = character.Title.GetRawText();
        return string.IsNullOrWhiteSpace(fallbackTitle)
            ? character.Id.Entry
            : fallbackTitle;
    }

    private static LocTable? GetChineseCharacterTable()
    {
        lock (SyncRoot)
        {
            if (_attemptedLoadChineseCharacterTable)
            {
                return _cachedChineseCharacterTable;
            }

            _attemptedLoadChineseCharacterTable = true;

            try
            {
                MethodInfo? loadTablesMethod = typeof(LocManager).GetMethod(
                    "LoadTablesFromPath",
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (loadTablesMethod == null)
                {
                    return null;
                }

                object? loadResult = loadTablesMethod.Invoke(null, new object?[] { "zhs", true });
                if (loadResult == null)
                {
                    return null;
                }

                Type resultType = loadResult.GetType();
                object? tablesObject = resultType.GetField("Item1")?.GetValue(loadResult)
                    ?? resultType.GetProperty("Item1")?.GetValue(loadResult);
                if (tablesObject is Dictionary<string, LocTable> tables &&
                    tables.TryGetValue("characters", out LocTable? characterTable))
                {
                    _cachedChineseCharacterTable = characterTable;
                }
            }
            catch (Exception ex)
            {
                MainFile.Logger.Warn($"Cloud analytics failed to load zh character localization table: {ex.Message}");
            }

            return _cachedChineseCharacterTable;
        }
    }

    private static List<string> GetInstalledModList()
    {
        try
        {
            return ModManager.Mods
                .Where(mod => mod.manifest?.id != null)
                .OrderBy(mod => mod.manifest!.id, StringComparer.Ordinal)
                .Select(BuildInstalledModDescriptor)
                .ToList();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Cloud analytics failed to enumerate installed mods: {ex.Message}");
            return [];
        }
    }

    private static string BuildInstalledModDescriptor(Mod mod)
    {
        string id = mod.manifest?.id ?? "unknown";
        string? name = mod.manifest?.name;
        string? version = mod.manifest?.version;
        string state = mod.state.ToString().ToLowerInvariant();

        string display = string.IsNullOrWhiteSpace(name) ||
            string.Equals(name, id, StringComparison.OrdinalIgnoreCase)
            ? id
            : $"{name} ({id})";
        return string.IsNullOrWhiteSpace(version)
            ? $"{display}[{state}]"
            : $"{display}@{version}[{state}]";
    }

    private static void SendIdentifySnapshot()
    {
        if (!CanCollect() || !_config.SendPersonProfiles)
        {
            return;
        }

        Dictionary<string, object?> snapshot;
        lock (SyncRoot)
        {
            snapshot = new Dictionary<string, object?>
            {
                ["mod_id"] = MainFile.ModId,
                ["mod_version"] = UpdateChecker.CurrentVersion,
                ["install_id"] = _state.InstallId,
                ["os_name"] = OS.GetName(),
                ["total_sessions_started"] = _state.TotalSessionsStarted,
                ["total_runs_started"] = _state.TotalRunsStarted,
                ["total_runs_won"] = _state.TotalRunsWon,
                ["total_runs_lost"] = _state.TotalRunsLost,
                ["total_pig_runs_started"] = _state.TotalPigRunsStarted,
                ["total_pig_runs_won"] = _state.TotalPigRunsWon,
                ["total_pig_runs_lost"] = _state.TotalPigRunsLost,
                ["last_session_started_utc"] = _state.LastSessionStartedUtc?.ToString("O"),
                ["last_mod_version"] = _state.LastModVersion
            };
        }

        var payload = new Dictionary<string, object?>
        {
            ["api_key"] = _config.ProjectApiKey,
            ["event"] = "$identify",
            ["distinct_id"] = _state.DistinctId,
            ["properties"] = new Dictionary<string, object?>
            {
                ["$set"] = snapshot
            },
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        };

        _ = SendPayloadAsync(payload, "$identify");
    }

    private static void SendEvent(string eventName, Dictionary<string, object?> properties)
    {
        if (!CanCollect())
        {
            return;
        }

        properties["mod_id"] = MainFile.ModId;
        properties["install_id"] = _state.InstallId;

        if (!_config.SendPersonProfiles)
        {
            properties["$process_person_profile"] = false;
        }

        var payload = new Dictionary<string, object?>
        {
            ["api_key"] = _config.ProjectApiKey,
            ["event"] = eventName,
            ["distinct_id"] = _state.DistinctId,
            ["properties"] = properties,
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        };

        _ = SendPayloadAsync(payload, eventName);
    }

    private static async Task SendPayloadAsync(Dictionary<string, object?> payload, string payloadName)
    {
        try
        {
            string url = BuildCaptureUrl(_config.Host);
            using var content = new System.Net.Http.StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using System.Net.Http.HttpResponseMessage response = await HttpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                MainFile.Logger.Warn($"Cloud analytics '{payloadName}' failed: {(int)response.StatusCode} {body}");
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Debug($"Cloud analytics '{payloadName}' exception: {ex.Message}");
        }
    }

    private static string BuildCaptureUrl(string host)
    {
        string trimmedHost = host.Trim().TrimEnd('/');
        if (trimmedHost.EndsWith(CapturePath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
        {
            return trimmedHost + "/";
        }
        return trimmedHost + CapturePath;
    }

    private static CloudAnalyticsConfig LoadConfig(string localConfigPath, string userConfigPath)
    {
        EnsureYamlConfigExists(userConfigPath, CloudAnalyticsConfigFile.CreateTemplate());

        CloudAnalyticsConfig config = new();

        if (TryReadYamlConfigFile(userConfigPath, out var fileConfig))
        {
            fileConfig.ApplyTo(config);
        }

        if (TryReadYamlConfigFile(localConfigPath, out var localConfig))
        {
            localConfig.ApplyTo(config);
        }

        string? envHost = System.Environment.GetEnvironmentVariable("YUWANCARD_POSTHOG_HOST");
        if (!string.IsNullOrWhiteSpace(envHost))
        {
            config.Host = envHost.Trim();
        }

        string? envApiKey = System.Environment.GetEnvironmentVariable("YUWANCARD_POSTHOG_API_KEY");
        if (!string.IsNullOrWhiteSpace(envApiKey))
        {
            config.ProjectApiKey = envApiKey.Trim();
        }

        string? envProjectId = System.Environment.GetEnvironmentVariable("YUWANCARD_POSTHOG_PROJECT_ID");
        if (!string.IsNullOrWhiteSpace(envProjectId))
        {
            config.ProjectId = envProjectId.Trim();
        }

        return config;
    }

    private static CloudAnalyticsState LoadState(string statePath)
    {
        if (!File.Exists(statePath))
        {
            return new CloudAnalyticsState();
        }

        try
        {
            string json = File.ReadAllText(statePath);
            return JsonSerializer.Deserialize<CloudAnalyticsState>(json, JsonOptions) ?? new CloudAnalyticsState();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to read analytics state '{statePath}': {ex.Message}");
            return new CloudAnalyticsState();
        }
    }

    private static void EnsureStableIds()
    {
        if (string.IsNullOrWhiteSpace(_state.DistinctId))
        {
            _state.DistinctId = Guid.NewGuid().ToString("N");
        }

        if (string.IsNullOrWhiteSpace(_state.InstallId))
        {
            _state.InstallId = Guid.NewGuid().ToString("N");
        }
    }

    private static string EnsureAnalyticsDirectory()
    {
        string analyticsDir = Path.Combine(OS.GetUserDataDir(), "mod_configs", MainFile.ModId);
        Directory.CreateDirectory(analyticsDir);
        return analyticsDir;
    }

    private static string GetLocalConfigPath()
    {
        try
        {
            string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string? assemblyDir = Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrWhiteSpace(assemblyDir))
            {
                return Path.Combine(assemblyDir, LocalConfigFileName);
            }
        }
        catch
        {
        }

        return LocalConfigFileName;
    }

    private static void SaveStateUnsafe()
    {
        WriteJsonFile(_statePath, _state);
    }

    private static void EnsureYamlConfigExists(string yamlPath, CloudAnalyticsConfigFile template)
    {
        if (File.Exists(yamlPath))
        {
            return;
        }

        WriteYamlConfigFile(yamlPath, template);
        MainFile.Logger.Info($"Created PostHog analytics template at '{yamlPath}'");
    }

    private static bool TryReadYamlConfigFile(string path, out CloudAnalyticsConfigFile config)
    {
        config = new CloudAnalyticsConfigFile();
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            config = ParseYamlConfig(File.ReadAllText(path));
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to read analytics YAML config '{path}': {ex.Message}");
            return false;
        }
    }

    private static CloudAnalyticsConfigFile ParseYamlConfig(string yaml)
    {
        var config = new CloudAnalyticsConfigFile();

        foreach (string rawLine in yaml.Replace("\r", string.Empty).Split('\n'))
        {
            string line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            int separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
            {
                continue;
            }

            string key = line[..separatorIndex].Trim();
            string rawValue = line[(separatorIndex + 1)..].Trim();
            ApplyYamlValue(config, key, rawValue);
        }

        return config;
    }

    private static void ApplyYamlValue(CloudAnalyticsConfigFile config, string key, string rawValue)
    {
        string value = NormalizeYamlScalar(rawValue);
        switch (key)
        {
            case "enabled":
                config.Enabled = ParseYamlBool(value, key);
                break;
            case "host":
                config.Host = value;
                break;
            case "projectApiKey":
                config.ProjectApiKey = value;
                break;
            case "projectId":
                config.ProjectId = value;
                break;
            case "captureLaunchEvents":
                config.CaptureLaunchEvents = ParseYamlBool(value, key);
                break;
            case "captureRunEvents":
                config.CaptureRunEvents = ParseYamlBool(value, key);
                break;
            case "sendPersonProfiles":
                config.SendPersonProfiles = ParseYamlBool(value, key);
                break;
        }
    }

    private static bool? ParseYamlBool(string value, string key)
    {
        if (bool.TryParse(value, out bool parsed))
        {
            return parsed;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        throw new FormatException($"Invalid boolean value '{value}' for key '{key}'.");
    }

    private static string NormalizeYamlScalar(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        string value = rawValue;
        if (!value.StartsWith('"') && !value.StartsWith('\''))
        {
            int commentIndex = value.IndexOf(" #", StringComparison.Ordinal);
            if (commentIndex >= 0)
            {
                value = value[..commentIndex].TrimEnd();
            }
        }

        if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
        {
            string inner = value[1..^1];
            if (value.StartsWith('"'))
            {
                return inner
                    .Replace("\\\"", "\"", StringComparison.Ordinal)
                    .Replace("\\\\", "\\", StringComparison.Ordinal);
            }

            return inner.Replace("''", "'", StringComparison.Ordinal);
        }

        if (value.Equals("null", StringComparison.OrdinalIgnoreCase) || value == "~")
        {
            return string.Empty;
        }

        return value;
    }

    private static void WriteYamlConfigFile(string path, CloudAnalyticsConfigFile config)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, SerializeYamlConfig(config));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to write analytics YAML file '{path}': {ex.Message}");
        }
    }

    private static string SerializeYamlConfig(CloudAnalyticsConfigFile config)
    {
        var lines = new List<string>();

        AppendYamlBool(lines, "enabled", config.Enabled);
        AppendYamlString(lines, "host", config.Host);
        AppendYamlString(lines, "projectApiKey", config.ProjectApiKey);
        AppendYamlString(lines, "projectId", config.ProjectId);
        AppendYamlBool(lines, "captureLaunchEvents", config.CaptureLaunchEvents);
        AppendYamlBool(lines, "captureRunEvents", config.CaptureRunEvents);
        AppendYamlBool(lines, "sendPersonProfiles", config.SendPersonProfiles);

        return string.Join(System.Environment.NewLine, lines) + System.Environment.NewLine;
    }

    private static void AppendYamlBool(List<string> lines, string key, bool? value)
    {
        if (!value.HasValue)
        {
            return;
        }

        lines.Add($"{key}: {value.Value.ToString().ToLowerInvariant()}");
    }

    private static void AppendYamlString(List<string> lines, string key, string? value)
    {
        if (value == null)
        {
            return;
        }

        string escaped = value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
        lines.Add($"{key}: \"{escaped}\"");
    }

    private static void WriteJsonFile<T>(string path, T data)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOptions));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to write analytics file '{path}': {ex.Message}");
        }
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        Initialize();
    }

    private static bool CanCollect()
    {
        lock (SyncRoot)
        {
            return CanCollectUnsafe();
        }
    }

    private static bool CanCollectUnsafe()
    {
        return _config.CanSendAnalytics
            && _state.AnalyticsConsentDecided
            && _state.AnalyticsCollectionEnabled;
    }

    private static bool IsMultiplayer(RunState runState)
    {
        RunManager? runManager = RunManager.Instance;
        var netService = runManager?.NetService;
        if (netService == null)
        {
            return runState.Players.Count > 1;
        }

        return netService.Type != NetGameType.Singleplayer
            && netService.Type != NetGameType.Replay;
    }

    private sealed class CloudAnalyticsConfig
    {
        public bool Enabled { get; set; }
        public string Host { get; set; } = DefaultHost;
        public string ProjectApiKey { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
        public bool CaptureLaunchEvents { get; set; } = true;
        public bool CaptureRunEvents { get; set; } = true;
        public bool SendPersonProfiles { get; set; } = true;

        public bool CanSendAnalytics =>
            Enabled
            && !string.IsNullOrWhiteSpace(Host)
            && !string.IsNullOrWhiteSpace(ProjectApiKey);
    }

    private sealed class CloudAnalyticsConfigFile
    {
        public bool? Enabled { get; set; }
        public string? Host { get; set; }
        public string? ProjectApiKey { get; set; }
        public string? ProjectId { get; set; }
        public bool? CaptureLaunchEvents { get; set; }
        public bool? CaptureRunEvents { get; set; }
        public bool? SendPersonProfiles { get; set; }

        public static CloudAnalyticsConfigFile CreateTemplate()
        {
            return new CloudAnalyticsConfigFile
            {
                Enabled = false,
                Host = DefaultHost,
                ProjectApiKey = string.Empty,
                ProjectId = string.Empty,
                CaptureLaunchEvents = true,
                CaptureRunEvents = true,
                SendPersonProfiles = true
            };
        }

        public void ApplyTo(CloudAnalyticsConfig config)
        {
            if (Enabled.HasValue)
            {
                config.Enabled = Enabled.Value;
            }

            if (!string.IsNullOrWhiteSpace(Host))
            {
                config.Host = Host.Trim();
            }

            if (!string.IsNullOrWhiteSpace(ProjectApiKey))
            {
                config.ProjectApiKey = ProjectApiKey.Trim();
            }

            if (!string.IsNullOrWhiteSpace(ProjectId))
            {
                config.ProjectId = ProjectId.Trim();
            }

            if (CaptureLaunchEvents.HasValue)
            {
                config.CaptureLaunchEvents = CaptureLaunchEvents.Value;
            }

            if (CaptureRunEvents.HasValue)
            {
                config.CaptureRunEvents = CaptureRunEvents.Value;
            }

            if (SendPersonProfiles.HasValue)
            {
                config.SendPersonProfiles = SendPersonProfiles.Value;
            }
        }
    }

    private sealed class CloudAnalyticsState
    {
        public string DistinctId { get; set; } = string.Empty;
        public string InstallId { get; set; } = string.Empty;
        public int TotalSessionsStarted { get; set; }
        public int TotalRunsStarted { get; set; }
        public int TotalRunsWon { get; set; }
        public int TotalRunsLost { get; set; }
        public int TotalPigRunsStarted { get; set; }
        public int TotalPigRunsWon { get; set; }
        public int TotalPigRunsLost { get; set; }
        public DateTime? LastSessionStartedUtc { get; set; }
        public string? LastModVersion { get; set; }
        public bool AnalyticsConsentDecided { get; set; }
        public bool AnalyticsCollectionEnabled { get; set; }
    }

    private sealed class ActiveRunTelemetry
    {
        public RunState? SourceRunState { get; set; }
        public required string RunId { get; set; }
        public required string CharacterId { get; set; }
        public required string CharacterType { get; set; }
        public required string CharacterNameZh { get; set; }
        public required DateTime StartedAtUtc { get; set; }
        public List<string> InstalledModList { get; set; } = [];
        public bool IsPig { get; set; }
        public bool IsMultiplayer { get; set; }
        public bool HasReportedEnd { get; set; }
        public int PlayerCount { get; set; }
        public int AscensionLevel { get; set; }
        public bool HasRingOfSevenCursesAtEnd { get; set; }
        public int MaliceLevelAtEnd { get; set; }
    }
}
