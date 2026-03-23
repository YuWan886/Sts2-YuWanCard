using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using YuWanCard.Monsters;

namespace YuWanCard.Encounters;

public sealed class KillerElite : EncounterModel
{
    public override RoomType RoomType => RoomType.Elite;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel> { ModelDb.Monster<Killer>() };

    public override float GetCameraScaling()
    {
        return 0.88f;
    }

    public override Vector2 GetCameraOffset()
    {
        return Vector2.Down * 50f;
    }

    public override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new List<(MonsterModel, string?)> { (ModelDb.Monster<Killer>().ToMutable(), null) };
    }
}
