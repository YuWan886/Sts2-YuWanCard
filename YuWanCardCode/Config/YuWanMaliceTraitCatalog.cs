using MegaCrit.Sts2.Core.Models;
using YuWanCard.Powers.MaliceTraits;

namespace YuWanCard.Config;

/// <summary>
/// Rarity tier of a malice trait. Higher tiers only unlock at higher malice levels.
/// </summary>
internal enum MaliceTraitRarity
{
    Common,
    Uncommon,
    Rare,
    Legendary
}

internal readonly record struct YuWanMaliceTraitDefinition(
    string Key,
    Type PowerType,
    MaliceTraitRarity Rarity);

/// <summary>
/// Single source of truth for the malice trait pool: which traits the
/// <see cref="Malice.MaliceTraitDistributor"/> may distribute, at which rarity tier,
/// and which key identifies them in the per-trait settings.
/// </summary>
internal static class YuWanMaliceTraitCatalog
{
    public const string SectionId = "malice_traits";
    public const int ButtonsPerRow = 4;

    /// <summary>
    /// Declared in distribution order (common → legendary). Keys are stable: they are the
    /// persistence identity inside malice_trait_settings.json and the multiplayer snapshot.
    /// </summary>
    public static readonly IReadOnlyList<YuWanMaliceTraitDefinition> Traits =
    [
        // Common — unlocked at malice 1
        new("tank_trait", typeof(TankTrait), MaliceTraitRarity.Common),
        new("speedy_trait", typeof(SpeedyTrait), MaliceTraitRarity.Common),
        new("regen_trait", typeof(RegenTrait), MaliceTraitRarity.Common),
        new("fiery_trait", typeof(FieryTrait), MaliceTraitRarity.Common),
        new("weakness_trait", typeof(WeaknessTrait), MaliceTraitRarity.Common),
        new("slowness_trait", typeof(SlownessTrait), MaliceTraitRarity.Common),
        new("gravity_trait", typeof(GravityTrait), MaliceTraitRarity.Common),
        new("vampiric_trait", typeof(VampiricTrait), MaliceTraitRarity.Common),
        new("bulwark_trait", typeof(BulwarkTrait), MaliceTraitRarity.Common),

        // Uncommon — unlocked at malice 4
        new("poison_trait", typeof(PoisonTrait), MaliceTraitRarity.Uncommon),
        new("resist_trait", typeof(ResistTrait), MaliceTraitRarity.Uncommon),
        new("protection_trait", typeof(ProtectionTrait), MaliceTraitRarity.Uncommon),
        new("blindness_trait", typeof(BlindnessTrait), MaliceTraitRarity.Uncommon),
        new("shulker_trait", typeof(ShulkerTrait), MaliceTraitRarity.Uncommon),
        new("expose_trait", typeof(ExposeTrait), MaliceTraitRarity.Uncommon),
        new("scorch_trait", typeof(ScorchTrait), MaliceTraitRarity.Uncommon),
        new("entangle_trait", typeof(EntangleTrait), MaliceTraitRarity.Uncommon),

        // Rare — unlocked at malice 6
        new("wither_trait", typeof(WitherTrait), MaliceTraitRarity.Rare),
        new("drain_trait", typeof(DrainTrait), MaliceTraitRarity.Rare),
        new("growth_trait", typeof(GrowthTrait), MaliceTraitRarity.Rare),
        new("counter_strike_trait", typeof(CounterStrikeTrait), MaliceTraitRarity.Rare),
        new("corrosion_trait", typeof(CorrosionTrait), MaliceTraitRarity.Rare),
        new("adaptive_trait", typeof(AdaptiveTrait), MaliceTraitRarity.Rare),
        new("invisible_trait", typeof(InvisibleTrait), MaliceTraitRarity.Rare),
        new("dispell_trait", typeof(DispellTrait), MaliceTraitRarity.Rare),
        new("steel_trait", typeof(SteelTrait), MaliceTraitRarity.Rare),
        new("berserk_trait", typeof(BerserkTrait), MaliceTraitRarity.Rare),
        new("phantom_trait", typeof(PhantomTrait), MaliceTraitRarity.Rare),

        // Legendary — unlocked at malice 9
        new("undying_trait", typeof(UndyingTrait), MaliceTraitRarity.Legendary),
        new("dementor_trait", typeof(DementorTrait), MaliceTraitRarity.Legendary),
        new("split_trait", typeof(SplitTrait), MaliceTraitRarity.Legendary),
        new("master_trait", typeof(MasterTrait), MaliceTraitRarity.Legendary),
        new("killer_aura_trait", typeof(KillerAuraTrait), MaliceTraitRarity.Legendary),
        new("ragnarok_trait", typeof(RagnarokTrait), MaliceTraitRarity.Legendary),
        new("cataclysm_trait", typeof(CataclysmTrait), MaliceTraitRarity.Legendary),
        new("enervate_trait", typeof(EnervateTrait), MaliceTraitRarity.Legendary)
    ];

    private static readonly Dictionary<Type, YuWanMaliceTraitDefinition> DefinitionsByType =
        Traits.ToDictionary(static definition => definition.PowerType);

    private static readonly Dictionary<string, YuWanMaliceTraitDefinition> DefinitionsByKey =
        Traits.ToDictionary(static definition => definition.Key, StringComparer.Ordinal);

    public static bool TryGetDefinition(Type powerType, out YuWanMaliceTraitDefinition definition)
        => DefinitionsByType.TryGetValue(powerType, out definition);

    public static bool TryGetDefinition(string key, out YuWanMaliceTraitDefinition definition)
        => DefinitionsByKey.TryGetValue(key, out definition);

    public static PowerModel CreateCanonicalPower(YuWanMaliceTraitDefinition definition)
        => ModelDb.GetById<PowerModel>(ModelDb.GetId(definition.PowerType));

    /// <summary>Localized trait name, falling back to the type name when localization is unavailable.</summary>
    public static string GetDisplayTitle(YuWanMaliceTraitDefinition definition)
    {
        try
        {
            string title = CreateCanonicalPower(definition).Title.GetRawText();
            if (!string.IsNullOrWhiteSpace(title))
            {
                return title;
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to resolve malice trait title for {definition.PowerType.Name}: {ex.Message}");
        }

        return definition.PowerType.Name;
    }
}
