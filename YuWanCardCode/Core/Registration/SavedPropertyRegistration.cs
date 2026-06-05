using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Core.Multiplayer;
using YuWanCard.Core.Patches;

namespace YuWanCard.Core.Registration;

internal static class SavedPropertyRegistration
{
    private static readonly object LockObj = new();
    private static readonly HashSet<Type> RegisteredTypes = [];

    public static void RegisterAssembly(Assembly assembly)
    {
        var savedPropertyTypes = AssemblyScanner.GetLoadableTypes(assembly)
            .Where(ShouldRegisterType)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        int registeredCount = 0;
        foreach (var type in savedPropertyTypes)
        {
            if (RegisterType(type))
            {
                registeredCount++;
            }
        }

        if (registeredCount > 0)
        {
            MainFile.Logger.Info(
                $"SavedPropertyRegistration: registered {registeredCount} custom model types");
        }
    }

    public static bool RegisterType(Type type)
    {
        lock (LockObj)
        {
            if (!RegisteredTypes.Add(type))
            {
                return false;
            }
        }

        SavedPropertiesTypeCachePatch.EnsureTypeRegistered(type);
        SavedPropertySyncRegistry.RegisterType(type);
        return true;
    }

    private static bool ShouldRegisterType(Type type)
    {
        return !type.IsAbstract
            && typeof(AbstractModel).IsAssignableFrom(type)
            && type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(property => property.GetCustomAttribute<SavedPropertyAttribute>() != null);
    }
}
