using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YuWanCard.Hextech;

namespace YuWanCard.Relics;

public sealed class PigletRechargeRune : HextechPigRuneBase
{
    private int _attackCardsPlayed;

    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Silver;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("AttackThreshold", 3),
        new EnergyVar(1)
    ];

    public override Task BeforeCombatStart()
    {
        _attackCardsPlayed = 0;
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
        {
            _attackCardsPlayed = 0;
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Attack)
        {
            return;
        }

        _attackCardsPlayed++;
        if (_attackCardsPlayed < DynamicVars["AttackThreshold"].IntValue)
        {
            return;
        }

        _attackCardsPlayed = 0;
        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner!);
    }
}
