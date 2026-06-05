using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace YuWanCard.Core.Abstracts;

public abstract class YuWanEncounterModel : EncounterModel, IYuWanContent
{
    public override RoomType RoomType { get; }

    protected YuWanEncounterModel(RoomType roomType)
    {
        RoomType = roomType;
    }

    public virtual string? CustomScenePath => null;

    public virtual string? CustomBackgroundScenePath => null;

    public override IEnumerable<string> ExtraAssetPaths
    {
        get
        {
            if (CustomBackgroundScenePath != null)
            {
                yield return CustomBackgroundScenePath;
            }
        }
    }

    public override bool HasScene => (CustomScenePath != null && ResourceLoader.Exists(CustomScenePath)) ||
                                     ResourceLoader.Exists(ScenePath);
}
