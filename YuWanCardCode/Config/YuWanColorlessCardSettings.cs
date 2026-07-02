using System.Text.Json;
using System.Text.Json.Serialization;
using YuWanCard.Core;

namespace YuWanCard.Config;

internal static class YuWanColorlessCardSettings
{
    private const string SaveFileName = "colorless_card_settings.json";
    private static readonly Lock Gate = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static Dictionary<string, bool>? _states;

    public static IReadOnlyDictionary<string, bool> SnapshotStates()
    {
        lock (Gate)
        {
            EnsureLoaded();
            return new Dictionary<string, bool>(_states!, StringComparer.Ordinal);
        }
    }

    public static bool IsEnabled(Type cardType)
    {
        if (!YuWanColorlessCardCatalog.TryGetDefinition(cardType, out var definition))
        {
            return true;
        }

        lock (Gate)
        {
            EnsureLoaded();
            return _states!.GetValueOrDefault(definition.Key, true);
        }
    }

    public static bool SetEnabled(string key, bool enabled)
    {
        lock (Gate)
        {
            EnsureLoaded();
            bool changed = !_states!.TryGetValue(key, out bool existing) || existing != enabled;
            _states[key] = enabled;
            if (changed)
            {
                SaveUnsafe();
            }

            return changed;
        }
    }

    public static bool SetAll(bool enabled)
    {
        lock (Gate)
        {
            EnsureLoaded();
            bool changed = false;
            foreach (var definition in YuWanColorlessCardCatalog.Cards)
            {
                if (_states!.TryGetValue(definition.Key, out bool existing) && existing == enabled)
                {
                    continue;
                }

                _states[definition.Key] = enabled;
                changed = true;
            }

            if (changed)
            {
                SaveUnsafe();
            }

            return changed;
        }
    }

    public static bool ApplySnapshot(IReadOnlyDictionary<string, bool> states)
    {
        lock (Gate)
        {
            EnsureLoaded();
            bool changed = false;
            foreach (var definition in YuWanColorlessCardCatalog.Cards)
            {
                bool enabled = states.GetValueOrDefault(definition.Key, true);
                if (_states!.TryGetValue(definition.Key, out bool existing) && existing == enabled)
                {
                    continue;
                }

                _states[definition.Key] = enabled;
                changed = true;
            }

            if (changed)
            {
                SaveUnsafe();
            }

            return changed;
        }
    }

    private static void EnsureLoaded()
    {
        if (_states != null)
        {
            return;
        }

        _states = CreateDefaultStateMap();
        string path = ResolveSavePath();
        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            string json = File.ReadAllText(path);
            var payload = JsonSerializer.Deserialize<SavePayload>(json, JsonOptions);
            if (payload?.EnabledCards == null)
            {
                return;
            }

            foreach (var (key, enabled) in payload.EnabledCards)
            {
                if (YuWanColorlessCardCatalog.TryGetDefinition(key, out _))
                {
                    _states[key] = enabled;
                }
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to load colorless card settings: {ex.Message}");
        }
    }

    private static void SaveUnsafe()
    {
        string path = ResolveSavePath();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var payload = new SavePayload
            {
                EnabledCards = new Dictionary<string, bool>(_states!, StringComparer.Ordinal)
            };
            string json = JsonSerializer.Serialize(payload, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to save colorless card settings: {ex.Message}");
        }
    }

    private static Dictionary<string, bool> CreateDefaultStateMap()
    {
        var result = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var definition in YuWanColorlessCardCatalog.Cards)
        {
            result[definition.Key] = true;
        }

        return result;
    }

    private static string ResolveSavePath()
        => YuWanModDataPathHelper.ResolveAccountFilePath(SaveFileName, "colorless card settings");

    private sealed class SavePayload
    {
        [JsonPropertyName("enabledCards")]
        public Dictionary<string, bool>? EnabledCards { get; set; }
    }
}
