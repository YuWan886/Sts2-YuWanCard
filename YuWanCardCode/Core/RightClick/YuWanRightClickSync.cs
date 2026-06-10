using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Multiplayer;

namespace YuWanCard.Core.RightClick;

internal readonly record struct YuWanRightClickManagedPayload(
    ulong OwnerNetId,
    YuWanRightClickModelKind Kind,
    MultiplayerModelIdentityToken ModelToken,
    YuWanRightClickTrigger Trigger,
    IReadOnlyList<YuWanRightClickBindingId> BindingIds);

internal static class YuWanRightClickManagedActions
{
    private const string ManagedActionModuleId = "yuwancard";
    private const string CombatActionKey = "right_click_combat";
    private const string NonCombatActionKey = "right_click_noncombat";
    private const string ManagedActionDisplayName = "YuWanCard.Core.RightClick.YuWanRightClickManagedGameAction";

    private static readonly YuWanManagedNetActionDescriptor<YuWanRightClickManagedPayload> CombatDescriptor =
        new(
            ManagedActionModuleId,
            CombatActionKey,
            SerializePayload,
            DeserializePayload,
            ExecuteManaged,
            GameActionType.CombatPlayPhaseOnly,
            ManagedActionDisplayName);

    private static readonly YuWanManagedNetActionDescriptor<YuWanRightClickManagedPayload> NonCombatDescriptor =
        new(
            ManagedActionModuleId,
            NonCombatActionKey,
            SerializePayload,
            DeserializePayload,
            ExecuteManaged,
            GameActionType.NonCombat,
            ManagedActionDisplayName);

    private static bool _registered;

    public static void EnsureRegistered()
    {
        if (_registered)
        {
            return;
        }

        YuWanManagedNetActions.Register(CombatDescriptor);
        YuWanManagedNetActions.Register(NonCombatDescriptor);
        _registered = true;
    }

    public static bool Request(
        RunManager? runManager,
        YuWanRightClickManagedPayload payload,
        ulong? ownerNetId = null)
    {
        EnsureRegistered();
        YuWanManagedNetActionDescriptor<YuWanRightClickManagedPayload> descriptor = CombatManager.Instance.IsInProgress
            ? CombatDescriptor
            : NonCombatDescriptor;
        return YuWanManagedNetActions.Request(runManager, descriptor, payload, ownerNetId);
    }

    private static async Task ExecuteManaged(YuWanManagedNetActionContext<YuWanRightClickManagedPayload> context)
    {
        try
        {
            if (context.Message.OwnerNetId != context.Player.NetId)
            {
                return;
            }

            await YuWanRightClickRegistry.ExecuteManagedPayload(
                context.Message,
                context.PlayerChoiceContext,
                context.Action);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"RightClick: managed action execution failed: {ex}");
        }
    }

    private static byte[] SerializePayload(YuWanRightClickManagedPayload payload)
    {
        var writer = new PacketWriter();
        writer.WriteULong(payload.OwnerNetId);
        writer.WriteEnum(payload.Kind);
        writer.WriteInt(payload.ModelToken.Identity.Value);
        writer.WriteFullModelId(payload.ModelToken.ModelId);
        writer.WriteBool(payload.Trigger.IsController);
        writer.WriteBool(payload.Trigger.Metadata != null);
        if (payload.Trigger.Metadata != null)
        {
            writer.WriteString(payload.Trigger.Metadata);
        }

        writer.WriteInt(payload.BindingIds.Count);
        foreach (YuWanRightClickBindingId bindingId in payload.BindingIds)
        {
            writer.WriteString(bindingId.Id);
        }

        return writer.Buffer[..(int)Math.Ceiling(writer.BitPosition / 8f)];
    }

    private static YuWanRightClickManagedPayload DeserializePayload(ReadOnlySpan<byte> bytes)
    {
        var reader = new PacketReader();
        reader.Reset(bytes.ToArray());

        ulong ownerNetId = reader.ReadULong();
        YuWanRightClickModelKind kind = reader.ReadEnum<YuWanRightClickModelKind>();
        var token = new MultiplayerModelIdentityToken(
            new MultiplayerModelIdentity(reader.ReadInt()),
            reader.ReadFullModelId());

        bool isController = reader.ReadBool();
        string? metadata = reader.ReadBool() ? reader.ReadString() : null;

        int bindingCount = reader.ReadInt();
        var bindingIds = new List<YuWanRightClickBindingId>(Math.Max(bindingCount, 0));
        for (int i = 0; i < bindingCount; i++)
        {
            string id = reader.ReadString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                bindingIds.Add(new YuWanRightClickBindingId(id.Trim()));
            }
        }

        return new YuWanRightClickManagedPayload(
            ownerNetId,
            kind,
            token,
            new YuWanRightClickTrigger(isController, metadata),
            bindingIds);
    }
}
