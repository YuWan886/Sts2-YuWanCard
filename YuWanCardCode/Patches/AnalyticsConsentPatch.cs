using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NMainMenu))]
public static class AnalyticsConsentPatch
{
    private static bool _consentPromptTriggered;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMainMenu._Ready))]
    public static void AfterMainMenuReady()
    {
        if (_consentPromptTriggered)
        {
            return;
        }

        if (!CloudAnalyticsService.ShouldShowConsentPrompt())
        {
            return;
        }

        _consentPromptTriggered = true;
        Callable.From(ShowConsentPopup).CallDeferred();
    }

    private static void ShowConsentPopup()
    {
        if (!CloudAnalyticsService.ShouldShowConsentPrompt())
        {
            return;
        }

        if (NModalContainer.Instance == null)
        {
            MainFile.Logger.Warn("NModalContainer not available, cannot show analytics consent popup");
            return;
        }

        var popup = AnalyticsConsentPopup.Create();
        if (popup == null)
        {
            return;
        }

        NModalContainer.Instance.Add(popup, showBackstop: true);
        MainFile.Logger.Info("Analytics consent popup shown");
    }
}
