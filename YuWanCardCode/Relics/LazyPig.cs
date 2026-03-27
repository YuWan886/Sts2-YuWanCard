using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class LazyPig : YuWanRelicModel
{
    private int _cardsPlayedThisTurn;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress;

    public override int DisplayAmount => _cardsPlayedThisTurn;

    public override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(5),
        new EnergyVar(1)
    ];

    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];

    private bool ShouldPreventCardPlay => _cardsPlayedThisTurn >= DynamicVars.Cards.IntValue;

    public LazyPig() : base(true)
    {
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }
        _cardsPlayedThisTurn = 0;
        InvokeDisplayAmountChanged();
        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, player);
    }

    public override bool ShouldPlay(CardModel card, AutoPlayType _)
    {
        if (card.Owner != Owner)
        {
            return true;
        }
        return !ShouldPreventCardPlay;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner)
        {
            _cardsPlayedThisTurn++;
        }
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
        {
            return Task.CompletedTask;
        }
        _cardsPlayedThisTurn = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _cardsPlayedThisTurn = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}
