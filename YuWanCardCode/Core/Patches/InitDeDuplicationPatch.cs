using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Exceptions;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Replaces ModelDb.Init with a version that gracefully handles duplicate types.
/// Also performs canonical instance registration (events, ancients, orbs, characters)
/// using type sets collected by ContentRegistry.RegisterAll, then freezes registrations.
/// </summary>
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
static class InitDeDuplicationPatch
{
    private static readonly FieldInfo? ContentByIdField =
        typeof(ModelDb).GetField("_contentById", BindingFlags.Static | BindingFlags.NonPublic);

    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    static bool SafeInit()
    {
        if (ContentByIdField?.GetValue(null) is not IDictionary<ModelId, AbstractModel> contentById) return true;

        var allTypes = ModelDb.AllAbstractModelSubtypes;
        int created = 0, skipped = 0;

        foreach (var type in allTypes)
        {
            var id = ModelDb.GetId(type);
            if (contentById.ContainsKey(id))
            {
                skipped++;
                continue;
            }

            try
            {
                var value = (AbstractModel)Activator.CreateInstance(type)!;
                contentById[id] = value;
                created++;

                RegisterCanonicalInstance(type, value);
            }
            catch (TargetInvocationException ex)
                when (ex.InnerException is DuplicateModelException)
            {
                skipped++;
            }
        }

        MainFile.Logger.Info(
            $"Init: {allTypes.Length} types, {created} created, {skipped} skipped");

        RunPostInitLogic();

        ContentRegistry.Freeze();
        ModLifecycle.Publish(ModLifecyclePhase.ContentFrozen);
        ModLifecycle.Publish(ModLifecyclePhase.ModelDbReady);

        return false;
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

    private static void RunPostInitLogic()
    {
        foreach (var modifier in Modifiers.YuWanModifierModel.RegisteredModifiers)
        {
            var modifierType = modifier.GetType();
            if (!ModelDb.Contains(modifierType))
            {
                ModelDb.Inject(modifierType);
            }
        }

        AutoRegisterCharacters();
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
