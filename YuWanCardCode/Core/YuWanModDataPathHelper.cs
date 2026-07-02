using Godot;
using MegaCrit.Sts2.Core.Saves;

namespace YuWanCard.Core;

internal static class YuWanModDataPathHelper
{
    private const string ModDataDirectory = "mod_data/YuWanCard";

    public static string ResolveAccountFilePath(string fileName, string settingsNameForLog)
    {
        try
        {
            string accountBasePath = UserDataPathProvider.GetAccountScopedBasePath(ModDataDirectory);
            return ProjectSettings.GlobalizePath($"{accountBasePath}/{fileName}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to resolve {settingsNameForLog} path from account-scoped storage: {ex.Message}");
            string appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "SlayTheSpire2", "default", "1", ModDataDirectory, fileName);
        }
    }
}
