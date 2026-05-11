using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;
using YuWanCard.Utils;

namespace YuWanCard.Characters;

public class PigAllCards : YuWanModifierModel
{
    public override LocString Title => new("modifiers", $"{Id.Entry}.title");
    public override LocString Description => new("modifiers", $"{Id.Entry}.description");

    public override Func<Task>? GenerateNeowOption(EventModel eventModel) => () => Task.CompletedTask;

    public override CardCreationOptions ModifyCardRewardCreationOptions(Player player, CardCreationOptions options)
    {
        if (player.Character is not Pig) return options;
        return PigCardPoolUtils.ModifyCardRewardOptions(player, options);
    }

    public override bool TryModifyCardRewardOptions(Player player, List<CardCreationResult> cardRewardOptions, CardCreationOptions creationOptions)
    {
        if (player.Character is not Pig)
        {
            return false;
        }

        return PigCardPoolUtils.TryNormalizePigCardRewardOptions(player, cardRewardOptions, creationOptions);
    }
}
