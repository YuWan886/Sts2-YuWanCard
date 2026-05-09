using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace YuWanCard.Monsters;

/// <summary>
/// Minimal visible creature used by CallCompanionsPower.
/// Set static fields immediately before calling <c>ModelDb.Monster&lt;T&gt;().ToMutable()</c>.
/// </summary>
public class CompanionPlaceholderModel : MonsterModel
{
    public static string? PendingVisualPath;
    public static int PendingHp = 1;
    public static string PendingDisplayName = "";

    public override int MinInitialHp => PendingHp;
    public override int MaxInitialHp => PendingHp;

    public override LocString Title => new("powers", $"{Id.Entry}.title");

    protected override string VisualsPath => PendingVisualPath ?? base.VisualsPath;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var idle = new MoveState("IDLE", _ => Task.CompletedTask);
        return new MonsterMoveStateMachine([idle], idle);
    }
}
