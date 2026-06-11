using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Powers;

public class EmperorsNewPigPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
        {
            return Task.CompletedTask;
        }

        List<CardModel> candidates = PileType.Hand.GetPile(player).Cards
            .Where(card => card.IsUpgradable)
            .ToList();
        if (candidates.Count == 0)
        {
            return Task.CompletedTask;
        }

        var rng = player.RunState.Rng.CombatCardGeneration;
        int upgrades = Math.Min((int)Amount, candidates.Count);
        if (upgrades <= 0)
        {
            return Task.CompletedTask;
        }

        Flash();
        for (int i = 0; i < upgrades; i++)
        {
            int index = rng.NextInt(candidates.Count);
            CardModel selectedCard = candidates[index];
            candidates.RemoveAt(index);
            CardCmd.Upgrade(selectedCard);
        }

        return Task.CompletedTask;
    }
}
