using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Multiplayer;
using YuWanCard.Relics;
using YuWanCard.UI;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NMerchantInventory))]
public static class TeammatePayShopPatch
{
    private static readonly Dictionary<NMerchantSlot, Button> _teammatePayButtons = new();
    private static readonly Dictionary<int, PendingPurchase> _pendingPurchases = new();
    private static bool _isInitialized = false;
    private static int _nextPurchaseId = 0;
    private static bool _isProcessingRequest = false;

    public class PendingPurchase
    {
        public int PurchaseId { get; set; }
        public NMerchantSlot Slot { get; set; } = null!;
        public MerchantEntry Entry { get; set; } = null!;
        public MerchantInventory? Inventory { get; set; }
        public int Cost { get; set; }
        public TeammatePayEntryType EntryType { get; set; }
        public ulong RequesterNetId { get; set; }
        public ulong TargetNetId { get; set; }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMerchantInventory._Ready))]
    public static void OnReady(NMerchantInventory __instance)
    {
        if (!_isInitialized)
        {
            TeammatePayMessageHandler.Register();
            TeammatePayMessageHandler.OnRequestReceived += OnPayRequestReceived;
            TeammatePayMessageHandler.OnResponseReceived += OnPayResponseReceived;
            _isInitialized = true;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMerchantInventory.Open))]
    public static void OnOpen(NMerchantInventory __instance)
    {
        if (!ShouldShowTeammatePayButtons()) return;
        AddTeammatePayButtons(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMerchantInventory.Close))]
    public static void OnClose(NMerchantInventory __instance)
    {
        RemoveAllTeammatePayButtons();
        _pendingPurchases.Clear();
        _isProcessingRequest = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMerchantInventory._ExitTree))]
    public static void OnExitTree(NMerchantInventory __instance)
    {
        RemoveAllTeammatePayButtons();
        _pendingPurchases.Clear();
        _isProcessingRequest = false;
    }

    private static bool ShouldShowTeammatePayButtons()
    {
        if (!TeammatePay.IsMultiplayerGame()) return false;
        var runState = RunManager.Instance?.State;
        return runState != null && runState.Players.Any(p => TeammatePay.HasTeammatePayRelic(p));
    }

    private static void AddTeammatePayButtons(NMerchantInventory inventory)
    {
        RemoveAllTeammatePayButtons();

        var slots = inventory.GetAllSlots();
        int index = 0;

        foreach (var slot in slots)
        {
            if (!slot.Entry.IsStocked) continue;
            if (slot is NMerchantCardRemoval) continue;

            var button = CreateTeammatePayButton(slot, index, inventory.Inventory);
            if (button != null)
            {
                _teammatePayButtons[slot] = button;
                slot.AddChild(button);
            }
            index++;
        }
    }

    private static Button? CreateTeammatePayButton(NMerchantSlot slot, int index, MerchantInventory? inventory)
    {
        var button = new Button
        {
            Name = "TeammatePayButton",
            Text = L10NLookup("YUWANCARD-TEAMMATE_PAY.button"),
            TooltipText = L10NLookup("YUWANCARD-TEAMMATE_PAY.button_tooltip"),
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            AnchorTop = 0f,
            AnchorBottom = 0f,
            OffsetLeft = -80f,
            OffsetRight = 80f,
            OffsetTop = -50f,
            OffsetBottom = -10f
        };
        button.AddThemeFontSizeOverride("font_size", 24);

        var entry = slot.Entry;
        int cost = entry.Cost;
        TeammatePayEntryType entryType = GetEntryType(slot);

        button.Pressed += async () => await OnTeammatePayButtonPressed(slot, entry, cost, index, entryType, inventory);
        return button;
    }

    private static TeammatePayEntryType GetEntryType(NMerchantSlot slot) => slot switch
    {
        NMerchantCard => TeammatePayEntryType.Card,
        NMerchantRelic => TeammatePayEntryType.Relic,
        NMerchantPotion => TeammatePayEntryType.Potion,
        NMerchantCardRemoval => TeammatePayEntryType.CardRemoval,
        _ => TeammatePayEntryType.Card
    };

    private static async Task OnTeammatePayButtonPressed(NMerchantSlot slot, MerchantEntry entry, int cost, int index, TeammatePayEntryType entryType, MerchantInventory? inventory)
    {
        if (_isProcessingRequest) return;

        var localPlayer = LocalContext.GetMe(RunManager.Instance?.State);
        if (localPlayer == null) return;

        _isProcessingRequest = true;

        try
        {
            var selectedTeammate = await ShowTeammateSelection(entry, cost);
            if (selectedTeammate == null) return;

            int purchaseId = _nextPurchaseId++;
            _pendingPurchases[purchaseId] = new PendingPurchase
            {
                PurchaseId = purchaseId,
                Slot = slot,
                Entry = entry,
                Inventory = inventory,
                Cost = cost,
                EntryType = entryType,
                RequesterNetId = localPlayer.NetId,
                TargetNetId = selectedTeammate.NetId
            };

            var request = new TeammatePayRequestMessage
            {
                PurchaseId = purchaseId,
                RequesterNetId = localPlayer.NetId,
                TargetNetId = selectedTeammate.NetId,
                GoldAmount = cost,
                EntryId = GetEntryId(entry),
                EntryName = GetEntryName(entry),
                EntryIndex = index,
                EntryType = entryType,
                Location = RunManager.Instance!.RunLocationTargetedBuffer.CurrentLocation
            };

            TeammatePayMessageHandler.SendRequest(request);
        }
        finally
        {
            _isProcessingRequest = false;
        }
    }

    private static string GetEntryId(MerchantEntry entry) => entry switch
    {
        MerchantCardEntry cardEntry => cardEntry.CreationResult?.Card?.Id?.Entry ?? "",
        MerchantRelicEntry relicEntry => relicEntry.Model?.Id?.Entry ?? "",
        MerchantPotionEntry potionEntry => potionEntry.Model?.Id?.Entry ?? "",
        MerchantCardRemovalEntry => "card_removal",
        _ => ""
    };

    private static async Task<Player?> ShowTeammateSelection(MerchantEntry entry, int cost)
    {
        var goldData = await TeammatePayMessageHandler.QueryAllTeammatesGold();
        var popup = TeammateSelectionPopup.Create(entry, cost, goldData);
        NModalContainer.Instance?.Add(popup);
        return await popup.WaitForSelection();
    }

    private static string GetEntryName(MerchantEntry entry) => entry switch
    {
        MerchantCardEntry cardEntry => cardEntry.CreationResult?.Card?.Title ?? "Card",
        MerchantRelicEntry relicEntry => relicEntry.Model?.Title?.GetFormattedText() ?? "Relic",
        MerchantPotionEntry potionEntry => potionEntry.Model?.Title?.GetFormattedText() ?? "Potion",
        MerchantCardRemovalEntry => L10NLookup("YUWANCARD-TEAMMATE_PAY.card_removal"),
        _ => "Item"
    };

    private static void OnPayRequestReceived(TeammatePayRequestMessage request)
    {
        var localPlayer = LocalContext.GetMe(RunManager.Instance?.State);
        if (localPlayer == null || localPlayer.NetId != request.TargetNetId) return;

        ShowPayRequestDialog(request);
    }

    private static void ShowPayRequestDialog(TeammatePayRequestMessage request)
    {
        var runState = RunManager.Instance?.State;
        if (runState == null) return;

        Player? requester = runState.Players.FirstOrDefault(p => p.NetId == request.RequesterNetId);
        if (requester == null) return;

        var dialog = TeammatePayRequestPopup.Create(request, requester);
        if (dialog != null)
        {
            NModalContainer.Instance?.AddChild(dialog);
        }
    }

    private static async void OnPayResponseReceived(TeammatePayResponseMessage response)
    {
        var localPlayer = LocalContext.GetMe(RunManager.Instance?.State);
        if (localPlayer == null || localPlayer.NetId != response.RequesterNetId) return;

        if (response.Accepted)
        {
            await ProcessPurchase(response);
        }
    }

    private static async Task<bool> ProcessPurchase(TeammatePayResponseMessage response)
    {
        if (!_pendingPurchases.TryGetValue(response.PurchaseId, out var purchase))
            return false;

        var entry = purchase.Entry;
        var inventory = purchase.Inventory;

        if (entry == null) return false;

        try
        {
            bool success = await entry.OnTryPurchaseWrapper(inventory, ignoreCost: true);

            if (success)
            {
                _pendingPurchases.Remove(response.PurchaseId);
            }

            return success;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"TeammatePay: Failed to complete purchase: {ex.Message}");
            return false;
        }
    }

    private static void RemoveAllTeammatePayButtons()
    {
        foreach (var kvp in _teammatePayButtons)
        {
            kvp.Value.QueueFree();
        }
        _teammatePayButtons.Clear();
    }

    private static string L10NLookup(string key) => new LocString("gameplay_ui", key).GetFormattedText();
}
