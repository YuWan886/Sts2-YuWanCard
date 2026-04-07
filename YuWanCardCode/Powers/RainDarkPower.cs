using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace YuWanCard.Powers;

public class RainDarkPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HealVar(6m),
        new DynamicVar("MaxHandSize", 10m)
    ];

    public int HealAfterCombat => DynamicVars["Heal"].IntValue;
    public int MaxHandSize => DynamicVars["MaxHandSize"].IntValue;

    public override async Task AfterEnergyReset(Player player)
    {
        if (player == Owner.Player && Owner.Player != null && Amount > 0)
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
            if (Amount > 0)
            {
                Flash();

                var hand = PileType.Hand.GetPile(player);
                int cardsToDraw = MaxHandSize - hand.Cards.Count;
                if (cardsToDraw > 0)
                {
                    await CardPileCmd.Draw(choiceContext, cardsToDraw, player);
                }

                Amount--;
            }
        }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner == null) return;
        
        var ownerName = Owner.Player?.Character?.Title?.ToString() ?? Owner.ToString() ?? "null";
        MainFile.Logger.Debug($"RainDarkPower.AfterCombatEnd called for {ownerName}, IsDead: {Owner.IsDead}, Amount: {Amount}");
        
        if (!Owner.IsDead)
        {
            Flash();
            await CreatureCmd.Heal(Owner, HealAfterCombat);
            MainFile.Logger.Info($"RainDarkPower healed {ownerName} for {HealAfterCombat} HP");
        }
    }
}
