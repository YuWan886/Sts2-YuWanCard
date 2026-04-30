using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Exceptions;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Replaces ModelDb.Init with a version that gracefully handles duplicate types.
/// Also explicitly runs all post-Init logic that would normally run as Harmony postfixes,
/// since returning false from this Prefix skips them.
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
            }
            catch (TargetInvocationException ex)
                when (ex.InnerException is DuplicateModelException)
            {
                skipped++;
            }
        }

        MainFile.Logger.Info(
            $"Init: {allTypes.Length} types, {created} created, {skipped} skipped");

        // Manually run post-Init logic that other Harmony postfixes would have done.
        RunPostInitLogic();

        return false; // skip original Init
    }

    /// <summary>
    /// Replicates essential post-Init logic from YuWanCard patches
    /// that would normally run as Harmony postfixes on ModelDb.Init.
    /// </summary>
    private static void RunPostInitLogic()
    {
        // 1. Register modifiers
        foreach (var modifier in Modifiers.YuWanModifierModel.RegisteredModifiers)
        {
            var modifierType = modifier.GetType();
            if (!ModelDb.Contains(modifierType))
            {
                ModelDb.Inject(modifierType);
            }
        }

        // 2. Auto-register all characters that implement IYuWanCharacter
        AutoRegisterCharacters();
    }

    /// <summary>
    /// Automatically registers all character models that implement IYuWanCharacter.
    /// This eliminates the need for manual registration in each mod.
    /// </summary>
    private static void AutoRegisterCharacters()
    {
        var characterTypes = ModelDb.AllAbstractModelSubtypes
            .Where(t => typeof(IYuWanCharacter).IsAssignableFrom(t) && !t.IsAbstract);

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
