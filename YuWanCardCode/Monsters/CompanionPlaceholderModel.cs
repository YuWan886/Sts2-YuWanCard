using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace YuWanCard.Monsters;

/// <summary>
/// Minimal visible creature used by CallCompanionsPower.
/// Its per-summon data belongs to the mutable model, never to shared static state.
/// </summary>
public class CompanionPlaceholderModel : MonsterModel
{
    public string? VisualPathOverride { get; set; }
    public int InitialHp { get; set; } = 1;

    public override int MinInitialHp => InitialHp;
    public override int MaxInitialHp => InitialHp;

    public override LocString Title => new("powers", "YUWANCARD-CALL_COMPANIONS_POWER.title");

    protected override string VisualsPath => VisualPathOverride ?? base.VisualsPath;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var idle = new MoveState("IDLE", _ => Task.CompletedTask);
        return new MonsterMoveStateMachine([idle], idle);
    }
}
