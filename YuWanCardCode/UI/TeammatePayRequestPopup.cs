using System.ComponentModel;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Multiplayer;

namespace YuWanCard.UI;

public partial class TeammatePayRequestPopup : Control, IScreenContext
{
    private const string ScenePath = "res://scenes/ui/vertical_popup.tscn";

    private NVerticalPopup? _verticalPopup;
    private TeammatePayRequestMessage _request;
    private Player? _requester;

    public Control? DefaultFocusedControl => null;

    public static TeammatePayRequestPopup? Create(TeammatePayRequestMessage request, Player requester)
    {
        var scene = GD.Load<PackedScene>(ScenePath);
        if (scene == null)
        {
            MainFile.Logger.Warn($"TeammatePay: Failed to load scene: {ScenePath}");
            return null;
        }

        var popup = new TeammatePayRequestPopup();
        popup._request = request;
        popup._requester = requester;
        popup.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        popup._verticalPopup = scene.Instantiate<NVerticalPopup>(PackedScene.GenEditState.Disabled);
        if (popup._verticalPopup == null)
        {
            MainFile.Logger.Warn("TeammatePay: Failed to instantiate NVerticalPopup");
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
        if (_verticalPopup == null || _requester == null) return;

        string title = new LocString("gameplay_ui", "YUWANCARD-TEAMMATE_PAY.pay_request_title").GetRawText();
        string requesterName = GetPlayerName(_requester);
        
        string fromTemplate = new LocString("gameplay_ui", "YUWANCARD-TEAMMATE_PAY.pay_request_from").GetRawText();
        string itemTemplate = new LocString("gameplay_ui", "YUWANCARD-TEAMMATE_PAY.pay_request_item").GetRawText();
        string costTemplate = new LocString("gameplay_ui", "YUWANCARD-TEAMMATE_PAY.pay_request_cost").GetRawText();
        
        string bodyText = $"{fromTemplate.Replace("{0}", requesterName)}\n\n" +
                          $"{itemTemplate.Replace("{0}", _request.EntryName)}\n" +
                          $"{costTemplate.Replace("{0}", _request.GoldAmount.ToString())}";

        _verticalPopup.SetText(title, bodyText);

        _verticalPopup.InitYesButton(
            new LocString("gameplay_ui", "YUWANCARD-TEAMMATE_PAY.accept"),
            OnAcceptPressed
        );

        _verticalPopup.InitNoButton(
            new LocString("gameplay_ui", "YUWANCARD-TEAMMATE_PAY.reject"),
            OnRejectPressed
        );
    }

    private string GetPlayerName(Player player)
    {
        return PlatformUtil.GetPlayerName(RunManager.Instance?.NetService?.Platform ?? PlatformType.None, player.NetId);
    }

    private async void OnAcceptPressed(NButton _)
    {
        bool deducted = await TeammatePayMessageHandler.DeductGoldLocally(_request.GoldAmount);
        if (!deducted)
        {
            MainFile.Logger.Warn("TeammatePay: Failed to deduct gold locally");
            return;
        }

        var response = new TeammatePayResponseMessage
        {
            PurchaseId = _request.PurchaseId,
            RequesterNetId = _request.RequesterNetId,
            ResponderNetId = _request.TargetNetId,
            Accepted = true,
            GoldAmount = _request.GoldAmount,
            EntryId = _request.EntryId,
            EntryIndex = _request.EntryIndex,
            EntryType = _request.EntryType,
            Location = _request.Location
        };

        TeammatePayMessageHandler.SendResponse(response);
        ClosePopup();
    }

    private void OnRejectPressed(NButton _)
    {
        var response = new TeammatePayResponseMessage
        {
            PurchaseId = _request.PurchaseId,
            RequesterNetId = _request.RequesterNetId,
            ResponderNetId = _request.TargetNetId,
            Accepted = false,
            GoldAmount = 0,
            EntryId = _request.EntryId,
            EntryIndex = _request.EntryIndex,
            EntryType = _request.EntryType,
            Location = _request.Location
        };

        TeammatePayMessageHandler.SendResponse(response);
        ClosePopup();
    }

    private void ClosePopup()
    {
        NModalContainer.Instance?.Clear();
        this.QueueFree();
    }
}
