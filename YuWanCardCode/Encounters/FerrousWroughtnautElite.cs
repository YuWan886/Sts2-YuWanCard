using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using YuWanCard.Core.Abstracts;
using YuWanCard.Monsters;

namespace YuWanCard.Encounters;

public sealed class FerrousWroughtnautElite : YuWanEncounterModel
{
    public FerrousWroughtnautElite() : base(RoomType.Elite)
    {
    }

    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<FerrousWroughtnaut>()];

    public bool IsValidForAct(ActModel act) => act is Glory;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return [(ModelDb.Monster<FerrousWroughtnaut>().ToMutable(), null)];
    }
}
