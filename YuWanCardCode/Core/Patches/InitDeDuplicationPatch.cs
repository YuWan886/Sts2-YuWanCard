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
        var contentById = ContentByIdField?.GetValue(null) as IDictionary<ModelId, AbstractModel>;
        if (contentById == null) return true;

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
        // 1. YuWanCard: Register modifiers
        foreach (var modifier in Modifiers.YuWanModifierModel.RegisteredModifiers)
        {
            var modifierType = modifier.GetType();
            if (!ModelDb.Contains(modifierType))
            {
                ModelDb.Inject(modifierType);
            }
        }

        // 2. YuWanCard: Register Pig character with ModelDbCharactersPatch
        // Use the canonical instance already created during Init, not a new one.
        var pig = ModelDb.Character<Characters.Pig>();
        ModelDbCharactersPatch.Register(pig);
    }
}
