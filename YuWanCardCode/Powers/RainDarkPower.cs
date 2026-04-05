using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;

namespace YuWanCard.Powers;

public class RainDarkPower : YuWanPowerModel
{
    private const int HealAfterCombat = 6;
    private const int MaxHandSize = 10;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterEnergyReset(Player player)
    {
        if (player == Owner.Player && Owner.Player != null)
        {
            Flash();
            int currentEnergy = Owner.Player.PlayerCombatState?.Energy ?? 0;
            if (currentEnergy > 0)
            {
                await PlayerCmd.GainEnergy(currentEnergy, Owner.Player);
            }
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player && Owner.Player != null)
        {
            Flash();

            var hand = PileType.Hand.GetPile(player);
            int cardsToDraw = MaxHandSize - hand.Cards.Count;
            if (cardsToDraw > 0)
            {
                await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), cardsToDraw, player);
            }

            await PowerCmd.TickDownDuration(this);
        }
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (!Owner.IsDead)
        {
            Flash();
            await CreatureCmd.Heal(Owner, HealAfterCombat);
        }
    }
}
