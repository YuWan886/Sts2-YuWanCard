using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using YuWanCard.Core.Abstracts;
using YuWanCard.Monsters;

namespace YuWanCard.Encounters;

[RegisterBoss(typeof(Glory))]
public sealed class IgnisBoss : YuWanEncounterModel
{
    public IgnisBoss() : base(RoomType.Boss)
    {
    }

    public override string CustomBgm => "event:/music/act3_boss_queen";

    public override string? CustomBackgroundScenePath =>
        "res://YuWanCard/scenes/backgrounds/ignis_boss/ignis_boss_background.tscn";

    public override string BossNodePath => "res://YuWanCard/images/map/placeholder/ignis_boss_icon";

    public override MegaSkeletonDataResource? BossNodeSpineResource => null;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<Ignis>()];

    public override float GetCameraScaling()
    {
        return 0.9f;
    }

    public override Vector2 GetCameraOffset()
    {
        return Vector2.Down * 70f;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return [(ModelDb.Monster<Ignis>().ToMutable(), null)];
    }
}
