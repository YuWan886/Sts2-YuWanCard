using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Config;
using YuWanCard.Core.Abstracts;
using YuWanCard.Relics;
using YuWanCard.Utils;

namespace YuWanCard.Events;

public sealed class SkullGoldRush : YuWanEventModel
{
    private const int DrawCost = 50;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar("DrawCost", DrawCost),
        new GoldVar("PrizeGold", DrawCost)
    ];

    public override bool IsAllowed(IRunState runState)
    {
        return YuWanContentAvailability.IsEventTypeEnabled<SkullGoldRush>();
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            new EventOption(
                this,
                ObtainSkullGoldRelic,
                $"{Id.Entry}.pages.INITIAL.options.OBTAIN_RELIC",
                HoverTipFactory.FromRelic<SkullGold>()),
            GenerateGambleOption()
        ];
    }

    private EventOption GenerateGambleOption()
    {
        if (Owner!.Gold >= DrawCost)
        {
            return new EventOption(this, Gamble, $"{Id.Entry}.pages.ALL.options.GAMBLE");
        }

        return new EventOption(this, null, $"{Id.Entry}.pages.ALL.options.GAMBLE_LOCKED");
    }

    private EventOption GenerateLeaveOption()
    {
        return new EventOption(this, Leave, $"{Id.Entry}.pages.ALL.options.LEAVE");
    }

    private async Task ObtainSkullGoldRelic()
    {
        AudioUtils.Play("res://YuWanCard/sounds/vfx/skull_gold_rush.mp3");
        await RelicCmd.Obtain<SkullGold>(Owner!);
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.RELIC_OBTAINED.description"));
    }

    private async Task Gamble()
    {
        AudioUtils.Play("res://YuWanCard/sounds/vfx/skull_gold_rush.mp3");
        var owner = Owner!;
        await PlayerCmd.LoseGold(DrawCost, owner, GoldLossType.Spent);

        int prizeGold = RollPrizeGold();
        DynamicVars["PrizeGold"].BaseValue = prizeGold;
        await PlayerCmd.GainGold(prizeGold, owner);

        bool canContinue = owner.Gold >= DrawCost;
        SetEventState(
            L10NLookup($"{Id.Entry}.pages.{(canContinue ? "DRAW_RESULT" : "BROKE")}.description"),
            [GenerateGambleOption(), GenerateLeaveOption()]);
    }

    private Task Leave()
    {
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.LEAVE.description"));
        return Task.CompletedTask;
    }

    private int RollPrizeGold()
    {
        int roll = Rng.NextInt(1000);
        return roll switch
        {
            < 50 => -999,
            < 130 => -100,
            < 260 => -50,
            < 400 => 1,
            < 540 => 20,
            < 660 => 30,
            < 760 => 50,
            < 840 => 100,
            < 880 => 999,
            _ => 0
        };
    }
}
