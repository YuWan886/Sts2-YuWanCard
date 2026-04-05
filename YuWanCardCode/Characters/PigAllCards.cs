using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;
using YuWanCard.Utils;

namespace YuWanCard.Characters;

public class PigAllCards : YuWanModifierModel
{
    public override LocString Title => new("modifiers", "YUWANCARD-PIG_ALL_CARDS.title");
    public override LocString Description => new("modifiers", "YUWANCARD-PIG_ALL_CARDS.description");

    public override Func<Task>? GenerateNeowOption(EventModel eventModel) => () => Task.CompletedTask;

    public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options)
    {
        if (player.Character is not Pig) return options;
        return PigCardPoolUtils.ModifyCardRewardOptions(player, options);
    }

    public override IEnumerable<CardModel> ModifyMerchantCardPool(Player player, IEnumerable<CardModel> options)
    {
        if (player.Character is not Pig) return options;
        return PigCardPoolUtils.ModifyMerchantCardPool(player, options);
    }
}
