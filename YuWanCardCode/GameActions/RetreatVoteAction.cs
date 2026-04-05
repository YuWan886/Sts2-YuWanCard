using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Encounters;
using YuWanCard.Patches;

namespace YuWanCard.GameActions;

public class RetreatVoteAction : GameAction
{
    private readonly Player _player;

    public override ulong OwnerId => _player.NetId;

    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;

    public RetreatVoteAction(Player player)
    {
        _player = player;
    }

    protected override async Task ExecuteAction()
    {
        if (_player.Creature.CombatState?.Encounter is not KillerElite killerElite)
            return;

        if (killerElite.HasPlayerVoted(_player.NetId))
            return;

        killerElite.AddVotedPlayer(_player.NetId);
        
        RefreshVoteUI();

        if (AllPlayersVoted(_player.Creature.CombatState, killerElite))
        {
            await ExecuteRetreat(_player.Creature.CombatState, killerElite);
        }
    }

    private void RefreshVoteUI()
    {
        if (NCombatRoom.Instance?.Ui != null)
        {
            var button = NCombatRoom.Instance.Ui.GetNodeOrNull<NRetreatButton>("YuWanRetreatButton");
            button?.CallDeferred(nameof(NRetreatButton.RefreshVotes));
        }
    }

    private bool AllPlayersVoted(CombatState combatState, KillerElite killerElite)
    {
        var votedPlayers = killerElite.GetVotedPlayers();
        foreach (var player in combatState.Players)
        {
            if (!votedPlayers.Contains(player.NetId))
            {
                return false;
            }
        }
        return true;
    }

    private async Task ExecuteRetreat(CombatState combatState, KillerElite killerElite)
    {
        killerElite.SetRetreated(true);

        foreach (var enemy in combatState.Enemies)
        {
            if (enemy.IsAlive)
            {
                await CreatureCmd.Escape(enemy);
            }
        }

        if (CombatManager.Instance != null && CombatManager.Instance.IsInProgress)
        {
            await CombatManager.Instance.CheckWinCondition();
        }
    }

    public override INetAction ToNetAction()
    {
        return new NetRetreatVoteAction();
    }

    public override string ToString()
    {
        return $"RetreatVoteAction {_player.NetId}";
    }
}

public struct NetRetreatVoteAction : INetAction, IPacketSerializable
{
    public GameAction ToGameAction(Player owner)
    {
        return new RetreatVoteAction(owner);
    }

    public void Serialize(PacketWriter writer)
    {
    }

    public void Deserialize(PacketReader reader)
    {
    }
}
