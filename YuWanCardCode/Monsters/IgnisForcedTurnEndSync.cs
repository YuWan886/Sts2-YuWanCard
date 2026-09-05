using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Core.Multiplayer;

namespace YuWanCard.Monsters;

/// <summary>
/// Descriptor for the host-authoritative Ignis forced turn end action.
/// Enqueued through <see cref="YuWanManagedNetActions"/> so it enters the shared
/// deterministic action queue on every peer: when a client detects the threshold,
/// the request travels to the host, which confirms it back to all peers. Execution
/// therefore happens at the same point in every peer's action ordering, regardless
/// of when the local HP change was observed.
/// </summary>
internal static class IgnisForcedTurnEndSync
{
    private const string ModuleId = "yuwancard";
    private const string ActionKey = "ignis_forced_turn_end";

    private static readonly YuWanManagedNetActionDescriptor<IgnisForcedTurnEndPayload> Descriptor =
        new(
            ModuleId,
            ActionKey,
            SerializePayload,
            DeserializePayload,
            ExecuteManaged,
            GameActionType.CombatPlayPhaseOnly,
            "YuWanCard.Monsters.IgnisForcedTurnEndAction");

    private static bool _registered;

    public static void EnsureRegistered()
    {
        if (_registered)
        {
            return;
        }

        YuWanManagedNetActions.Register(Descriptor);
        _registered = true;
    }

    /// <summary>
    /// Requests a synchronized forced turn end. Uses the game's synchronized enqueue
    /// path: on host it is enqueued immediately and announced to clients; on client it
    /// is sent as a request which the host confirms back, so every peer enqueues it in
    /// the same deterministic position.
    /// </summary>
    public static bool Request(RunManager? runManager = null)
    {
        EnsureRegistered();
        return YuWanManagedNetActions.Request(
            runManager ?? RunManager.Instance,
            Descriptor,
            new IgnisForcedTurnEndPayload());
    }

    private static async Task ExecuteManaged(YuWanManagedNetActionContext<IgnisForcedTurnEndPayload> context)
    {
        // Running inside the shared action queue on every peer. PlayerCmd.EndTurn is
        // idempotent (CombatManager.IsPlayerReadyToEndTurn guard), so peers that have
        // already readied up simply no-op; the host's transition then proceeds once,
        // deterministically, after the queues settle.
        await IgnisForcedTurnEndAction.EndTurnForAllPlayers();
    }

    private static byte[] SerializePayload(IgnisForcedTurnEndPayload payload)
    {
        return [];
    }

    private static IgnisForcedTurnEndPayload DeserializePayload(ReadOnlySpan<byte> bytes)
    {
        return new IgnisForcedTurnEndPayload();
    }
}

internal readonly record struct IgnisForcedTurnEndPayload();

/// <summary>
/// The GameAction side of the managed action. Kept as a separate type so the
/// end-turn logic can also be invoked directly in singleplayer without the
/// managed-action plumbing.
/// </summary>
public static class IgnisForcedTurnEndAction
{
    internal static Task EndTurnForAllPlayers()
    {
        CombatState? combatState = CombatManager.Instance.DebugOnlyGetState();
        if (combatState == null || combatState.CurrentSide != CombatSide.Player)
        {
            return Task.CompletedTask;
        }

        foreach (Player player in combatState.Players)
        {
            PlayerCmd.EndTurn(player, canBackOut: false);
        }

        return Task.CompletedTask;
    }
}
