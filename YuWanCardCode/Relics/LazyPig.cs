using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public class LazyPig : YuWanRelicModel
{
    [SavedProperty]
    private int _turnCount;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress;

    public override int DisplayAmount => _turnCount;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DexterityPower>()];

    public LazyPig() : base(true)
    {
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }
        _turnCount++;
        InvokeDisplayAmountChanged();
        if (_turnCount % 2 == 0)
        {
            Flash();
            await PowerCmd.Apply<DexterityPower>(choiceContext, player.Creature, 2, player.Creature, null);
        }
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
        {
            return Task.CompletedTask;
        }
        _turnCount = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _turnCount = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override void ModifyShuffleOrder(Player player, List<CardModel> cards, bool isInitialShuffle)
    {
        if (player != Owner)
        {
            return;
        }

        var attackCards = new List<CardModel>();
        var nonAttackCards = new List<CardModel>();

        foreach (var card in cards)
        {
            if (card.Type == CardType.Attack)
            {
                attackCards.Add(card);
            }
            else
            {
                nonAttackCards.Add(card);
            }
        }

        nonAttackCards.StableShuffle(Owner.RunState.Rng.Shuffle);
        attackCards.StableShuffle(Owner.RunState.Rng.Shuffle);

        var result = new List<CardModel>();
        int nonAttackIndex = 0;
        int attackIndex = 0;

        while (nonAttackIndex < nonAttackCards.Count || attackIndex < attackCards.Count)
        {
            if (nonAttackIndex < nonAttackCards.Count)
            {
                result.Add(nonAttackCards[nonAttackIndex]);
                nonAttackIndex++;
            }

            if (attackIndex < attackCards.Count && Owner.RunState.Rng.Niche.NextFloat() >= 0.5f)
            {
                result.Add(attackCards[attackIndex]);
                attackIndex++;
            }
        }

        while (attackIndex < attackCards.Count)
        {
            result.Add(attackCards[attackIndex]);
            attackIndex++;
        }

        cards.Clear();
        cards.AddRange(result);
    }
}
