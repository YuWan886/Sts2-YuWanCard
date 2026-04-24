using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.HoverTips;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public class GreedyPig : YuWanRelicModel
{
    [SavedProperty]
    private bool HasAddedGreedThisCombat { get; set; }

    private GoldModificationGuard? _goldGuard;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<Greed>()
    ];

    private GoldModificationGuard GoldGuard => _goldGuard ??= new GoldModificationGuard(
        () => Owner,
        amount => Math.Floor(amount * 1m),
        async amount =>
        {
            Flash();
            await PlayerCmd.GainGold(amount, Owner!);
        }
    );

    public GreedyPig() : base(true)
    {
    }

    public override RelicModel? GetUpgradeReplacement() => null;

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
        {
            return Task.CompletedTask;
        }

        HasAddedGreedThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        if (HasAddedGreedThisCombat || Owner?.Creature?.CombatState == null)
        {
            return;
        }

        HasAddedGreedThisCombat = true;
        Flash();

        CardModel greedCard = ModelDb.Card<Greed>();
        CardModel card = Owner.Creature.CombatState.CreateCard(greedCard, Owner);

        var hand = PileType.Hand.GetPile(Owner);
        bool isHandFull = hand.Cards.Count >= 10;

        if (isHandFull)
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
        }
        else
        {
            _ = await CardPileCmd.AddGeneratedCardsToCombat([card], PileType.Hand, Owner);
        }
    }

    public override bool ShouldGainGold(decimal amount, Player player)
    {
        return GoldGuard.ShouldGainGold(amount, player);
    }

    public override async Task AfterGoldGained(Player player)
    {
        await GoldGuard.AfterGoldGained(player);
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        await base.AfterCombatVictory(room);
        HasAddedGreedThisCombat = false;
    }
}
