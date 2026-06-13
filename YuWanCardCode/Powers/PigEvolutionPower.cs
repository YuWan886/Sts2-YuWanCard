using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Core.Abstracts;

namespace YuWanCard.Powers;

public class PigEvolutionPower : YuWanPowerModel
{
    private sealed class Data
    {
        public bool TriggeredThisTurn;
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return Task.CompletedTask;
        }

        GetInternalData<Data>().TriggeredThisTurn = false;

        List<CardModel> candidates = PileType.Hand.GetPile(player).Cards
            .Where(card => card.IsUpgradable)
            .ToList();
        if (candidates.Count == 0)
        {
            return Task.CompletedTask;
        }

        int index = player.RunState.Rng.CombatCardGeneration.NextInt(candidates.Count);
        CardCmd.Upgrade(candidates[index]);
        Flash();
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player || !cardPlay.Card.IsUpgraded)
        {
            return;
        }

        var data = GetInternalData<Data>();
        if (data.TriggeredThisTurn)
        {
            return;
        }

        data.TriggeredThisTurn = true;
        Flash();
        await CardPileCmd.Draw(context, (int)Amount, Owner.Player!);
    }
}
