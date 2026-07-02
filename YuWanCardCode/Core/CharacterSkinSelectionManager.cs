using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core;

public sealed record YuWanCharacterSkinDefinition(
    string Id,
    string DisplayNameLocKey,
    string? VisualPath = null,
    string? MerchantAnimPath = null,
    string? IconTexturePath = null,
    string? IconOutlineTexturePath = null);

public interface IYuWanCharacterSkinProvider
{
    IReadOnlyList<YuWanCharacterSkinDefinition> CharacterSkins { get; }
}

public static class CharacterSkinSelectionManager
{
    private const string SaveFileName = "character_skin_settings.json";
    private static readonly Lock Gate = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };
    private static Dictionary<string, string>? _selectedSkinIds;

    public static bool HasSelectableSkins(CharacterModel character)
        => GetSkins(character).Count > 1;

    public static IReadOnlyList<YuWanCharacterSkinDefinition> GetSkins(CharacterModel character)
        => character is IYuWanCharacterSkinProvider provider
            ? provider.CharacterSkins
            : [];

    public static YuWanCharacterSkinDefinition? GetSelectedSkin(CharacterModel character)
    {
        IReadOnlyList<YuWanCharacterSkinDefinition> skins = GetSkins(character);
        if (skins.Count == 0)
        {
            return null;
        }

        lock (Gate)
        {
            EnsureLoaded();
            string characterKey = GetCharacterKey(character);
            if (_selectedSkinIds!.TryGetValue(characterKey, out string? selectedSkinId))
            {
                YuWanCharacterSkinDefinition? selected = skins.FirstOrDefault(skin =>
                    string.Equals(skin.Id, selectedSkinId, StringComparison.OrdinalIgnoreCase));
                if (selected != null)
                {
                    return selected;
                }

                _selectedSkinIds.Remove(characterKey);
                SaveUnsafe();
            }

            return skins[0];
        }
    }

    public static bool TryCycleSkin(CharacterModel character, int delta)
    {
        IReadOnlyList<YuWanCharacterSkinDefinition> skins = GetSkins(character);
        if (skins.Count <= 1 || delta == 0)
        {
            return false;
        }

        lock (Gate)
        {
            EnsureLoaded();

            string characterKey = GetCharacterKey(character);
            int currentIndex = 0;
            if (_selectedSkinIds!.TryGetValue(characterKey, out string? selectedSkinId))
            {
                for (int i = 0; i < skins.Count; i++)
                {
                    if (!string.Equals(skins[i].Id, selectedSkinId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = (currentIndex + delta) % skins.Count;
            if (nextIndex < 0)
            {
                nextIndex += skins.Count;
            }

            if (nextIndex == currentIndex)
            {
                return false;
            }

            _selectedSkinIds[characterKey] = skins[nextIndex].Id;
            SaveUnsafe();
            return true;
        }
    }

    public static string ResolveVisualPath(CharacterModel character, string fallbackPath)
        => ResolveOverride(character, static skin => skin.VisualPath, fallbackPath);

    public static string ResolveMerchantAnimPath(CharacterModel character, string fallbackPath)
        => ResolveOverride(character, static skin => skin.MerchantAnimPath, fallbackPath);

    public static string ResolveIconTexturePath(CharacterModel character, string fallbackPath)
        => ResolveOverride(character, static skin => skin.IconTexturePath, fallbackPath);

    public static string ResolveIconOutlineTexturePath(CharacterModel character, string fallbackPath)
        => ResolveOverride(character, static skin => skin.IconOutlineTexturePath, fallbackPath);

    private static string ResolveOverride(
        CharacterModel character,
        Func<YuWanCharacterSkinDefinition, string?> selector,
        string fallbackPath)
    {
        string? overridePath = selector(GetSelectedSkin(character) ?? NullSkin);
        return string.IsNullOrWhiteSpace(overridePath)
            ? fallbackPath
            : overridePath;
    }

    private static readonly YuWanCharacterSkinDefinition NullSkin = new(
        Id: "default",
        DisplayNameLocKey: string.Empty);

    private static void EnsureLoaded()
    {
        if (_selectedSkinIds != null)
        {
            return;
        }

        _selectedSkinIds = new(StringComparer.OrdinalIgnoreCase);
        string path = ResolveSavePath();
        try
        {
            if (!File.Exists(path))
            {
                return;
            }

            string json = File.ReadAllText(path);
            SavePayload? payload = JsonSerializer.Deserialize<SavePayload>(json, JsonOptions);
            if (payload?.SelectedSkins == null)
            {
                return;
            }

            foreach (var (characterKey, skinId) in payload.SelectedSkins)
            {
                if (string.IsNullOrWhiteSpace(characterKey) || string.IsNullOrWhiteSpace(skinId))
                {
                    continue;
                }

                _selectedSkinIds[characterKey] = skinId;
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to load character skin settings: {ex}");
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
                SelectedSkins = new Dictionary<string, string>(_selectedSkinIds!, StringComparer.OrdinalIgnoreCase)
            };
            string json = JsonSerializer.Serialize(payload, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to save character skin settings: {ex}");
        }
    }

    private static string GetCharacterKey(CharacterModel character)
        => character.Id.ToString();

    private static string ResolveSavePath()
        => YuWanModDataPathHelper.ResolveAccountFilePath(SaveFileName, "character skin settings");

    private sealed class SavePayload
    {
        [JsonPropertyName("selectedSkins")]
        public Dictionary<string, string>? SelectedSkins { get; set; }
    }
}
