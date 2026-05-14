using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Cards;
using YuWanCard.Utils;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Powers;

public class VictoryBreedsArrogancePower : YuWanPowerModel
{
    private const string ForcedMoveId = "YUWANCARD_VICTORY_BREEDS_ARROGANCE_DEFEND";

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://YuWanCard/images/powers/sad_army_win_power.png";
    public override string? CustomBigIconPath => CustomPackedIconPath;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BlockAmount", 15m)];

    [SavedProperty]
    public int YUWANCARD_BlockAmount
    {
        get => DynamicVars["BlockAmount"].IntValue;
        set => DynamicVars["BlockAmount"].BaseValue = value;
    }

    [SavedProperty]
    public int YUWANCARD_LastForcedRound { get; set; } = -1;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (CombatState == null || CombatState.CurrentSide != CombatSide.Player || Amount <= 0)
        {
            return Task.CompletedTask;
        }

        if (YUWANCARD_LastForcedRound == CombatState.RoundNumber)
        {
            return Task.CompletedTask;
        }

        ForceDefendIntent();
        return Task.CompletedTask;
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (cardSource is VictoryBreedsArrogance card)
        {
            YUWANCARD_BlockAmount = card.DynamicVars.Block.IntValue;
        }

        ForceDefendIntent();
        return Task.CompletedTask;
    }

    private void ForceDefendIntent()
    {
        if (!Owner.IsAlive || Owner.Monster == null || CombatState == null)
        {
            return;
        }

        var followUpStateId = IntentUtils.GetCurrentMoveFollowUpStateId(Owner) ?? Owner.Monster.NextMove.Id;
        var forcedMove = new MoveState(ForcedMoveId, PerformForcedDefend, new DefendIntent())
        {
            FollowUpStateId = followUpStateId,
            MustPerformOnceBeforeTransitioning = true
        };

        Owner.Monster.SetMoveImmediate(forcedMove);
        YUWANCARD_LastForcedRound = CombatState.RoundNumber;
    }

    private async Task PerformForcedDefend(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Owner, YUWANCARD_BlockAmount, ValueProp.Move, null);

        if (Amount > 0)
        {
            await PowerCmd.Decrement(this);
        }
    }
}
