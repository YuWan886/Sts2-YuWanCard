using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Timeline;
using YuWanCard.Cards;
using YuWanCard.Potions;
using YuWanCard.Relics;

namespace YuWanCard.Timeline.Epochs;

public sealed class Pig1Epoch : PigEpochBase
{
    public const string EpochId = "PIG1_EPOCH";

    public override string Id => EpochId;

    public override EpochEra Era => EpochEra.Invitation2;

    public override int EraPosition => 3;

    public override string UnlockText => new LocString("epochs", $"{Id}.unlockText").GetFormattedText();

    public override EpochModel[] GetTimelineExpansion() =>
    [
        EpochModel.Get(Pig2Epoch.EpochId),
        EpochModel.Get(Pig3Epoch.EpochId),
        EpochModel.Get(Pig4Epoch.EpochId),
        EpochModel.Get(Pig5Epoch.EpochId),
        EpochModel.Get(Pig6Epoch.EpochId),
        EpochModel.Get(Pig7Epoch.EpochId)
    ];

    public override void QueueUnlocks()
    {
        string text = new LocString("epochs", $"{Id}.unlock").GetFormattedText();
        NTimelineScreen.Instance.QueueMiscUnlock(text);
        QueueTimelineExpansion(GetTimelineExpansion());
    }
}

public sealed class Pig2Epoch : PigEpochBase
{
    public const string EpochId = "PIG2_EPOCH";

    public override string Id => EpochId;

    public override EpochEra Era => EpochEra.Flourish0;

    public override int EraPosition => 3;

    public static IReadOnlyList<CardModel> Cards =>
    [
        ModelDb.Card<PigEat>(),
        ModelDb.Card<PigRiceMeal>(),
        ModelDb.Card<PigBurger>()
    ];

    public override string UnlockText => CreateCardUnlockText(Cards.ToList());

    public override void QueueUnlocks()
    {
        NTimelineScreen.Instance.QueueCardUnlock(Cards);
    }
}

public sealed class Pig3Epoch : PigEpochBase
{
    public const string EpochId = "PIG3_EPOCH";

    public override string Id => EpochId;

    public override EpochEra Era => EpochEra.Flourish2;

    public override int EraPosition => 2;

    public static IReadOnlyList<RelicModel> Relics =>
    [
        ModelDb.Relic<SoftWarmth>(),
        ModelDb.Relic<PiggyDoll>(),
        ModelDb.Relic<TankPig>()
    ];

    public override string UnlockText => new LocString("epochs", $"{Id}.unlockText").GetFormattedText();

    public override void QueueUnlocks()
    {
        NTimelineScreen.Instance.QueueRelicUnlock(Relics.ToList());
    }
}

public sealed class Pig4Epoch : PigEpochBase
{
    public const string EpochId = "PIG4_EPOCH";

    public override string Id => EpochId;

    public override EpochEra Era => EpochEra.Flourish3;

    public override int EraPosition => 4;

    public static List<PotionModel> Potions =>
    [
        ModelDb.Potion<CarrotFeastPotion>(),
        ModelDb.Potion<SweetDreamPotion>(),
        ModelDb.Potion<OinkChargePotion>()
    ];

    public override string UnlockText => CreatePotionUnlockText(Potions);

    public override void QueueUnlocks()
    {
        NTimelineScreen.Instance.QueuePotionUnlock(Potions);
        string text = new LocString("epochs", $"{Id}.unlock").GetFormattedText();
        NTimelineScreen.Instance.QueueMiscUnlock(text);
    }
}

public sealed class Pig5Epoch : PigEpochBase
{
    public const string EpochId = "PIG5_EPOCH";

    public override string Id => EpochId;

    public override EpochEra Era => EpochEra.Blight1;

    public override int EraPosition => 4;

    public static IReadOnlyList<CardModel> Cards =>
    [
        ModelDb.Card<PigUnity>(),
        ModelDb.Card<PigCall>(),
        ModelDb.Card<ManyPigs>()
    ];

    public override string UnlockText => CreateCardUnlockText(Cards.ToList());

    public override void QueueUnlocks()
    {
        NTimelineScreen.Instance.QueueCardUnlock(Cards);
    }
}

public sealed class Pig6Epoch : PigEpochBase
{
    public const string EpochId = "PIG6_EPOCH";

    public override string Id => EpochId;

    public override EpochEra Era => EpochEra.Blight2;

    public override int EraPosition => 2;

    public static IReadOnlyList<RelicModel> Relics =>
    [
        ModelDb.Relic<AllIWant>(),
        ModelDb.Relic<ShoppingCart>(),
        ModelDb.Relic<GreedyPig>()
    ];

    public override string UnlockText => new LocString("epochs", $"{Id}.unlockText").GetFormattedText();

    public override void QueueUnlocks()
    {
        NTimelineScreen.Instance.QueueRelicUnlock(Relics.ToList());
    }
}

public sealed class Pig7Epoch : PigEpochBase
{
    public const string EpochId = "PIG7_EPOCH";

    public override string Id => EpochId;

    public override EpochEra Era => EpochEra.Invitation7;

    public override int EraPosition => 2;

    public static IReadOnlyList<CardModel> Cards =>
    [
        ModelDb.Card<PigRoar>(),
        ModelDb.Card<PigClimbTower>(),
        ModelDb.Card<PigLeader>()
    ];

    public override string UnlockText => CreateCardUnlockText(Cards.ToList());

    public override void QueueUnlocks()
    {
        NTimelineScreen.Instance.QueueCardUnlock(Cards);
    }
}
