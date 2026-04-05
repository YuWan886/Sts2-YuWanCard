using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using YuWanCard.Monsters;
using BaseLib.Utils;

namespace YuWanCard.Encounters;

public sealed class KillerElite : EncounterModel
{
    private static readonly SavedSpireField<EncounterModel, bool> RetreatedField = new(() => false, "KillerElite_Retreated");
    private static readonly SpireField<EncounterModel, HashSet<ulong>> VotedPlayersField = new(() => []);

    public override RoomType RoomType => RoomType.Elite;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel> { ModelDb.Monster<Killer>() };

    public override bool ShouldGiveRewards => !RetreatedField.Get(this);

    public void SetRetreated(bool value) => RetreatedField.Set(this, value);

    public HashSet<ulong> GetVotedPlayers() => VotedPlayersField.Get(this) ?? [];
    
    public void AddVotedPlayer(ulong netId)
    {
        var voted = VotedPlayersField.Get(this) ?? [];
        if (!voted.Contains(netId))
        {
            voted.Add(netId);
            VotedPlayersField.Set(this, voted);
        }
    }
    
    public bool HasPlayerVoted(ulong netId)
    {
        var voted = VotedPlayersField.Get(this);
        return voted != null && voted.Contains(netId);
    }
    
    public void ClearVotes()
    {
        VotedPlayersField.Set(this, []);
    }

    public override float GetCameraScaling()
    {
        return 0.88f;
    }

    public override Vector2 GetCameraOffset()
    {
        return Vector2.Down * 50f;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)> { (ModelDb.Monster<Killer>().ToMutable(), null) };
    }
}
