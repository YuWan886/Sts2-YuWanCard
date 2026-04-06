using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using YuWanCard.Config;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NMainMenu))]
public static class UpdateCheckPatch
{
    private static bool _updateCheckTriggered = false;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMainMenu._Ready))]
    public static void AfterMainMenuReady(NMainMenu __instance)
    {
        if (_updateCheckTriggered)
        {
            return;
        }

        if (!YuWanCardConfig.EnableAutoUpdateCheck)
        {
            MainFile.Logger.Debug("Auto update check is disabled");
            return;
        }

        _updateCheckTriggered = true;

        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);

            var result = await UpdateChecker.CheckForUpdateAsync();

            if (result.AlreadyChecked)
            {
                return;
            }

            if (!result.Success)
            {
                MainFile.Logger.Debug("Update check failed or no update available");
                return;
            }

            if (result.HasUpdate)
            {
                MainFile.Logger.Info($"Update available: {result.CurrentVersion} -> {result.LatestVersion}");
                ShowUpdatePopupOnMainThread(result);
            }
            else
            {
                MainFile.Logger.Debug("No update available");
            }
        });
    }

    private static void ShowUpdatePopupOnMainThread(UpdateCheckResult result)
    {
        Callable.From(() => ShowUpdatePopup(result)).CallDeferred();
    }

    private static void ShowUpdatePopup(UpdateCheckResult result)
    {
        if (NModalContainer.Instance == null)
        {
            MainFile.Logger.Warn("NModalContainer not available, cannot show update popup");
            return;
        }

        var popup = UpdatePopup.Create(result.CurrentVersion, result.LatestVersion, result.ReleaseUrl);
        if (popup != null)
        {
            NModalContainer.Instance.Add(popup, showBackstop: true);
            MainFile.Logger.Info("Update popup shown");
        }
    }

    public static void ResetTriggerState()
    {
        _updateCheckTriggered = false;
        UpdateChecker.ResetCheckState();
    }
}
