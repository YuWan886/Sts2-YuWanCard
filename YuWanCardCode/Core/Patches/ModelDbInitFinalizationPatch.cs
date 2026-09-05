using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Finalizes canonical instance registration after ModelDb.Init has created all models.
/// </summary>
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
static class ModelDbInitFinalizationPatch
{
    [HarmonyPostfix]
    static void FinalizeModelDb()
    {
        RegisterExistingCanonicalInstances();
        AutoRegisterCharacters();

        ContentRegistry.Freeze();
        ModLifecycle.Publish(ModLifecyclePhase.ContentFrozen);
        ModLifecycle.Publish(ModLifecyclePhase.ModelDbReady);
    }

    /// <summary>
    /// Registers a canonical instance with the appropriate system based on
    /// registration attributes collected during ContentRegistry.RegisterAll.
    /// </summary>
    private static void RegisterCanonicalInstance(Type type, AbstractModel instance)
    {
        if (ContentRegistry.EventTypes.Contains(type) && instance is EventModel eventModel)
            CustomEventRegistry.Register(eventModel);

        if (ContentRegistry.AncientTypes.Contains(type) && instance is AncientEventModel ancient)
            CustomAncientRegistry.Register(ancient);

        if (ContentRegistry.CharacterTypes.Contains(type) && instance is CharacterModel character)
            ModelDbCharactersPatch.Register(character);

        if (ContentRegistry.RelicPoolTypes.Contains(type) && instance is RelicPoolModel relicPool)
            CustomRelicPoolRegistry.Register(relicPool);
    }

    /// <summary>
    /// Registers tracked types from the canonical instances created by ModelDb.Init.
    /// </summary>
    private static void RegisterExistingCanonicalInstances()
    {
        foreach (var type in ModelDb.AllAbstractModelSubtypes)
        {
            if (!ContentRegistry.EventTypes.Contains(type)
                && !ContentRegistry.AncientTypes.Contains(type)
                && !ContentRegistry.CharacterTypes.Contains(type)
                && !ContentRegistry.RelicPoolTypes.Contains(type))
            {
                continue;
            }

            try
            {
                var instance = ModelDb.GetByIdOrNull<AbstractModel>(ModelDb.GetId(type));
                if (instance != null)
                    RegisterCanonicalInstance(type, instance);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Warn(
                    $"Failed to backfill canonical registration for {type.Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Auto-registers characters implementing IYuWanCharacter that don't
    /// have an explicit [RegisterCharacter] attribute.
    /// </summary>
    private static void AutoRegisterCharacters()
    {
        var characterTypes = ModelDb.AllAbstractModelSubtypes
            .Where(t => typeof(IYuWanCharacter).IsAssignableFrom(t) && !t.IsAbstract &&
                        !ContentRegistry.CharacterTypes.Contains(t));

        foreach (var characterType in characterTypes)
        {
            try
            {
                var character = ModelDb.GetById<CharacterModel>(ModelDb.GetId(characterType));
                if (character != null)
                {
                    ModelDbCharactersPatch.Register(character);
                    MainFile.Logger.Info($"Auto-registered character: {characterType.Name}");
                }
            }
            catch (Exception ex)
            {
                MainFile.Logger.Error($"Failed to auto-register character {characterType.Name}: {ex.Message}");
            }
        }
    }
}

/// <summary>
/// Deduplicates AllAbstractModelSubtypes to prevent "Two AbstractModels ... share an ID" warnings
/// in ModelIdSerializationCache when the same type appears both in the base assembly and mods.
/// </summary>
[HarmonyPatch(typeof(ModelDb), "AllAbstractModelSubtypes", MethodType.Getter)]
static class AllAbstractModelSubtypesDedupPatch
{
    static void Postfix(ref Type[] __result)
    {
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);
        var deduped = new List<Type>(__result.Length);

        foreach (var type in __result)
        {
            string key = GetTypeIdentityKey(type);
            if (!seenKeys.Add(key))
            {
                MainFile.Logger.Warn($"Deduplicated duplicate AbstractModel subtype registration: {key}");
                continue;
            }

            deduped.Add(type);
        }

        __result = deduped.ToArray();
    }

    private static string GetTypeIdentityKey(Type type)
    {
        string assemblyName = type.Assembly.GetName().Name ?? "<unknown>";
        string fullName = type.FullName ?? type.Name;
        return $"{assemblyName}:{fullName}";
    }
}
