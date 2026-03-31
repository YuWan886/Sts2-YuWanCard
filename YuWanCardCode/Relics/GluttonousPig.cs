using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public class GluttonousPig : YuWanRelicModel
{
    [SavedProperty]
    private int CardsEatenCount { get; set; }

    [SavedProperty]
    private bool HasAddedBuffThisCombat { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    public override int DisplayAmount => CardsEatenCount;

    public GluttonousPig() : base(true)
    {
    }

    public override RelicModel? GetUpgradeReplacement() => null;

    public override bool TryModifyCardRewardAlternatives(Player player, CardReward cardReward, List<CardRewardAlternative> alternatives)
    {
        if (base.Owner != player)
        {
            return false;
        }
        alternatives.Add(new CardRewardAlternative("EAT", OnEatCard, PostAlternateCardRewardAction.DismissScreenAndRemoveReward));
        return true;
    }

    private async Task OnEatCard()
    {
        EatCard();
        await Task.CompletedTask;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
        {
            return Task.CompletedTask;
        }

        HasAddedBuffThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        if (HasAddedBuffThisCombat || Owner?.Creature?.CombatState == null)
        {
            return;
        }

        // 每吃掉 2 次，获得 1 层覆甲和 1 点力量
        int buffStacks = CardsEatenCount / 2;
        if (buffStacks > 0)
        {
            HasAddedBuffThisCombat = true;
            Flash();

            await PowerCmd.Apply<PlatingPower>(Owner.Creature, buffStacks, Owner.Creature, null);
            await PowerCmd.Apply<StrengthPower>(Owner.Creature, buffStacks, Owner.Creature, null);

            MainFile.Logger.Info($"GluttonousPig: Applied {buffStacks} Plating and Strength (eaten {CardsEatenCount} cards)");
        }
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        await base.AfterCombatVictory(room);
        HasAddedBuffThisCombat = false;
    }

    // 当玩家吃掉卡牌时调用
    public void EatCard()
    {
        CardsEatenCount++;
        InvokeDisplayAmountChanged();
        MainFile.Logger.Info($"GluttonousPig: Ate a card, total eaten: {CardsEatenCount}");
    }
}
