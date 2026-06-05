using System.Reflection;

namespace YuWanCard.Core.Registration;

/// <summary>
/// Safely loads types from an assembly, catching ReflectionTypeLoadException
/// which can occur on Mono/IL2CPP platforms (Android, iOS) or for assemblies
/// with unloadable types (e.g. native interop assemblies like Steamworks.NET).
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
            int loaderErrorCount = ex.LoaderExceptions.Count(e => e != null);
            if (loaderErrorCount > 0)
                MainFile.Logger.Info(
                    $"[AssemblyScanner] {loaderErrorCount} type(s) skipped in {assembly.GetName().Name} " +
                    $"(first: {ex.LoaderExceptions.First(e => e != null)!.Message})");
            return ex.Types.Where(t => t != null).Cast<Type>().ToArray();
        }
    }
}
