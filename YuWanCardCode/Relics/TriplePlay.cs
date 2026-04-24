using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class TriplePlay : YuWanRelicModel
{
    private CardType? _lastCardType;
    private int _consecutiveCount;

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override bool ShowCounter => _consecutiveCount > 0 && CombatManager.Instance.IsInProgress;

    public override int DisplayAmount => _consecutiveCount;

    public TriplePlay() : base(true)
    {
    }

    public override Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side == Owner.Creature.Side)
        {
            _lastCardType = null;
            _consecutiveCount = 0;
            InvokeDisplayAmountChanged();
        }
        return Task.CompletedTask;
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner != Owner)
        {
            return playCount;
        }

        var cardType = card.Type;
        if (cardType == CardType.Status || cardType == CardType.Curse)
        {
            return playCount;
        }

        if (_lastCardType == cardType)
        {
            _consecutiveCount++;
        }
        else
        {
            _lastCardType = cardType;
            _consecutiveCount = 1;
        }
        InvokeDisplayAmountChanged();

        if (_consecutiveCount == 3)
        {
            Flash();
            _consecutiveCount = 0;
            return playCount + 1;
        }

        return playCount;
    }
}
