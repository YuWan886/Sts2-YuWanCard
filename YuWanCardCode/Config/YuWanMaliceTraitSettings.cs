using System.Text.Json;
using System.Text.Json.Serialization;

namespace YuWanCard.Config;

/// <summary>
/// Per-trait on/off switches for the malice difficulty system. A disabled trait is never
/// handed out by the <see cref="Malice.MaliceTraitDistributor"/>; it keeps its rarity tier
/// and weight, so re-enabling it restores the original distribution behaviour.
/// </summary>
internal static class YuWanMaliceTraitSettings
{
    private const string SaveFileName = "malice_trait_settings.json";
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

    public static bool IsEnabled(Type traitType)
    {
        if (!YuWanMaliceTraitCatalog.TryGetDefinition(traitType, out var definition))
        {
            return true;
        }

        lock (Gate)
        {
            EnsureLoaded();
            return _states!.GetValueOrDefault(definition.Key, true);
        }
    }

    public static bool IsEnabled(string key)
    {
        lock (Gate)
        {
            EnsureLoaded();
            return _states!.GetValueOrDefault(key, true);
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
            foreach (var definition in YuWanMaliceTraitCatalog.Traits)
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
            foreach (var definition in YuWanMaliceTraitCatalog.Traits)
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
            if (payload?.EnabledTraits == null)
            {
                return;
            }

            foreach (var (key, enabled) in payload.EnabledTraits)
            {
                if (YuWanMaliceTraitCatalog.TryGetDefinition(key, out _))
                {
                    _states[key] = enabled;
                }
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to load malice trait settings: {ex.Message}");
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
                EnabledTraits = new Dictionary<string, bool>(_states!, StringComparer.Ordinal)
            };
            string json = JsonSerializer.Serialize(payload, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to save malice trait settings: {ex.Message}");
        }
    }

    private static Dictionary<string, bool> CreateDefaultStateMap()
    {
        var result = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var definition in YuWanMaliceTraitCatalog.Traits)
        {
            result[definition.Key] = true;
        }

        return result;
    }

    private static string ResolveSavePath()
        => YuWanModDataPathHelper.ResolveAccountFilePath(SaveFileName, "malice trait settings");

    private sealed class SavePayload
    {
        [JsonPropertyName("enabledTraits")]
        public Dictionary<string, bool>? EnabledTraits { get; set; }
    }
}
