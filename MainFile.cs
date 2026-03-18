using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves.Runs;
using System.Linq;
using System.Reflection;

namespace YuWanCard;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YuWanCard";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);

        harmony.PatchAll();

        InjectSavedPropertyTypes();
    }

    private static void InjectSavedPropertyTypes()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var typesWithSavedProperty = assembly.GetTypes()
            .Where(t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(p => p.GetCustomAttribute<SavedPropertyAttribute>() != null));

        foreach (var type in typesWithSavedProperty)
        {
            SavedPropertiesTypeCache.InjectTypeIntoCache(type);
            Logger.Info($"Injected SavedProperty type into cache: {type.Name}");
        }
    }
}
