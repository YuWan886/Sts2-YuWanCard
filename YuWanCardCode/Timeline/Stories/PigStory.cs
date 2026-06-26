using MegaCrit.Sts2.Core.Timeline;
using YuWanCard.Timeline.Epochs;

namespace YuWanCard.Timeline.Stories;

public sealed class PigStory : StoryModel
{
    protected override string Id => PigTimelineRegistry.StoryKey;

    public override EpochModel[] Epochs =>
    [
        EpochModel.Get(Pig1Epoch.EpochId),
        EpochModel.Get(Pig2Epoch.EpochId),
        EpochModel.Get(Pig3Epoch.EpochId),
        EpochModel.Get(Pig4Epoch.EpochId),
        EpochModel.Get(Pig5Epoch.EpochId),
        EpochModel.Get(Pig6Epoch.EpochId),
        EpochModel.Get(Pig7Epoch.EpochId)
    ];
}
