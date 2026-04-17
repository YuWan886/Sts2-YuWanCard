using System.ComponentModel;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace YuWanCard.Utils;

public partial class UpdatePopup : Control, IScreenContext
{
    private const string ScenePath = "res://scenes/ui/vertical_popup.tscn";

    private NVerticalPopup? _verticalPopup;
    private string _releaseUrl = string.Empty;
    private string _currentVersion = string.Empty;
    private string _latestVersion = string.Empty;

    public Control? DefaultFocusedControl => null;

    public static UpdatePopup? Create(string currentVersion, string latestVersion, string releaseUrl)
    {
        var scene = GD.Load<PackedScene>(ScenePath);
        if (scene == null)
        {
            MainFile.Logger.Warn($"Failed to load scene: {ScenePath}");
            return null;
        }

        var popup = new UpdatePopup();
        popup._releaseUrl = releaseUrl;
        popup._currentVersion = currentVersion;
        popup._latestVersion = latestVersion;
        popup.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        popup._verticalPopup = scene.Instantiate<NVerticalPopup>(PackedScene.GenEditState.Disabled);
        if (popup._verticalPopup == null)
        {
            MainFile.Logger.Warn("Failed to instantiate NVerticalPopup");
            return null;
        }

        popup.AddChild(popup._verticalPopup);
        return popup;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void _Ready()
    {
        base._Ready();
        _SetupContent();
    }

    private void _SetupContent()
    {
        if (_verticalPopup == null) return;

        var titleLoc = new LocString("settings_ui", "YUWANCARD-UPDATE_POPUP.title");
        var bodyLoc = new LocString("settings_ui", "YUWANCARD-UPDATE_POPUP.body");

        string rawBody = bodyLoc.GetRawText();
        string bodyText = rawBody
            .Replace("{0}", _currentVersion)
            .Replace("{1}", _latestVersion)
            .Replace("{2}", BaseLibVersion.Version);

        _verticalPopup.SetText(titleLoc.GetRawText(), bodyText);

        _verticalPopup.InitYesButton(
            new LocString("settings_ui", "YUWANCARD-UPDATE_POPUP.download"),
            OnDownloadPressed
        );

        _verticalPopup.InitNoButton(
            new LocString("settings_ui", "YUWANCARD-UPDATE_POPUP.close"),
            OnClosePressed
        );

    }

    private void OnDownloadPressed(NButton _)
    {
        MainFile.Logger.Info($"Opening release URL: {_releaseUrl}");
        OS.ShellOpen(_releaseUrl);
        ClosePopup();
    }

    private void OnClosePressed(NButton _)
    {
        ClosePopup();
    }

    private void ClosePopup()
    {
        NModalContainer.Instance?.Clear();
        this.QueueFreeSafely();
    }
}
