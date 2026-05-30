using System.Text.Json.Serialization;

namespace YuWanCard.Malice;

public sealed class MaliceProgressData
{
    [JsonPropertyName("characters")]
    public Dictionary<string, MaliceCharacterProgress> Characters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class MaliceCharacterProgress
{
    [JsonPropertyName("max_malice")]
    public int MaxMalice { get; set; }

    [JsonPropertyName("preferred_malice")]
    public int PreferredMalice { get; set; }
}
