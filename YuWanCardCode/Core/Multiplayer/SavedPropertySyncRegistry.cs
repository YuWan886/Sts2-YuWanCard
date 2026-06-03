using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Core.Multiplayer;

internal static class SavedPropertySyncRegistry
{
    private static readonly object Gate = new();
    private static readonly Harmony SetterHarmony = new($"{MainFile.ModId}.saved_property_sync.setters");
    private static readonly Dictionary<Type, SavedPropertySyncMetadata> MetadataByType = [];
    private static readonly HashSet<MethodBase> PatchedSetters = [];
    private static readonly HarmonyMethod SetterPostfix = new(typeof(SavedPropertySyncRegistry), nameof(OnSavedPropertySet));

    public static void RegisterType(Type type)
    {
        if (!ShouldRegisterType(type))
        {
            return;
        }

        var properties = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(property => property.DeclaringType == type)
            .Where(property => property.GetCustomAttribute<SavedPropertyAttribute>() != null)
            .Where(property => property.CanRead && property.SetMethod != null)
            .Where(property => property.GetIndexParameters().Length == 0)
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();

        if (properties.Length == 0)
        {
            return;
        }

        lock (Gate)
        {
            MetadataByType[type] = new SavedPropertySyncMetadata(type, properties);

            foreach (PropertyInfo property in properties)
            {
                MethodInfo? setter = property.SetMethod;
                if (setter == null || !PatchedSetters.Add(setter))
                {
                    continue;
                }

                SetterHarmony.Patch(setter, postfix: SetterPostfix);
            }
        }
    }

    public static bool IsRegisteredModel(AbstractModel? model)
    {
        return model != null && IsRegisteredType(model.GetType());
    }

    public static bool IsRegisteredType(Type type)
    {
        lock (Gate)
        {
            return MetadataByType.ContainsKey(type);
        }
    }

    private static void OnSavedPropertySet(object __instance)
    {
        if (__instance is AbstractModel model)
        {
            SavedPropertyMultiplayerSync.NotifyPotentialStateChange(model);
        }
    }

    private static bool ShouldRegisterType(Type type)
    {
        return typeof(MegaCrit.Sts2.Core.Models.CardModel).IsAssignableFrom(type)
               || typeof(MegaCrit.Sts2.Core.Models.RelicModel).IsAssignableFrom(type);
    }

    private sealed record SavedPropertySyncMetadata(Type Type, IReadOnlyList<PropertyInfo> Properties);
}
