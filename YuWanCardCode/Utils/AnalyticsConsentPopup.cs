using System.ComponentModel;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace YuWanCard.Utils;

public partial class AnalyticsConsentPopup : Control, IScreenContext
{
    private const string ScenePath = "res://scenes/ui/vertical_popup.tscn";

    private NVerticalPopup? _verticalPopup;

    public Control? DefaultFocusedControl => null;

    public static AnalyticsConsentPopup? Create()
    {
        var scene = GD.Load<PackedScene>(ScenePath);
        if (scene == null)
        {
            MainFile.Logger.Warn($"Failed to load scene: {ScenePath}");
            return null;
        }

        var popup = new AnalyticsConsentPopup();
        popup.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        popup._verticalPopup = scene.Instantiate<NVerticalPopup>(PackedScene.GenEditState.Disabled);
        if (popup._verticalPopup == null)
        {
            MainFile.Logger.Warn("Failed to instantiate analytics consent popup");
            return null;
        }

        popup.AddChild(popup._verticalPopup);
        return popup;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void _Ready()
    {
        base._Ready();
        SetupContent();
    }

    private void SetupContent()
    {
        if (_verticalPopup == null)
        {
            return;
        }

        string title = new LocString("settings_ui", "YUWANCARD-ANALYTICS_POPUP.title").GetRawText();
        string body = new LocString("settings_ui", "YUWANCARD-ANALYTICS_POPUP.body").GetRawText();

        _verticalPopup.SetText(title, body);
        _verticalPopup.InitYesButton(
            new LocString("settings_ui", "YUWANCARD-ANALYTICS_POPUP.enable"),
            OnEnablePressed
        );
        _verticalPopup.InitNoButton(
            new LocString("settings_ui", "YUWANCARD-ANALYTICS_POPUP.disable"),
            OnDisablePressed
        );
    }

    private void OnEnablePressed(NButton _)
    {
        CloudAnalyticsService.SetCollectionEnabled(true);
        ClosePopup();
    }

    private void OnDisablePressed(NButton _)
    {
        CloudAnalyticsService.SetCollectionEnabled(false);
        ClosePopup();
    }

    private void ClosePopup()
    {
        NModalContainer.Instance?.Clear();
        this.QueueFree();
    }
}
