using System.Reflection;

namespace YuWanCard.Core.Registration;

/// <summary>
/// Safely loads types from an assembly, catching ReflectionTypeLoadException
/// which can occur on Mono/IL2CPP platforms (Android, iOS).
/// </summary>
internal static class AssemblyScanner
{
    public static IReadOnlyList<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            foreach (var loaderEx in ex.LoaderExceptions.Where(e => e != null))
                MainFile.Logger.Warn(
                    $"[AssemblyScanner] Loader exception in {assembly.GetName().Name}: {loaderEx!.Message}");
            return ex.Types.Where(t => t != null).Cast<Type>().ToArray();
        }
    }
}
