using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace YuWanCard.Powers;

public class RainDarkPower : YuWanPowerModel
{
    private const int HealAfterCombat = 6;
    private Player? _subscribedPlayer;
    private bool _isProcessing;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        var player = Owner.Player;
        if (player != null && player.PlayerCombatState != null)
        {
            _subscribedPlayer = player;
            player.PlayerCombatState.EnergyChanged += OnEnergyChanged;
        }
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature owner)
    {
        if (_subscribedPlayer != null && _subscribedPlayer.PlayerCombatState != null)
        {
            _subscribedPlayer.PlayerCombatState.EnergyChanged -= OnEnergyChanged;
        }
        return Task.CompletedTask;
    }

    private void OnEnergyChanged(int oldEnergy, int newEnergy)
    {
        if (_isProcessing) return;

        if (newEnergy > oldEnergy && _subscribedPlayer != null)
        {
            _isProcessing = true;
            try
            {
                int gained = newEnergy - oldEnergy;
                _subscribedPlayer.PlayerCombatState?.GainEnergy(gained);
            }
            finally
            {
                _isProcessing = false;
            }
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == Owner.Side)
        {
            await PowerCmd.TickDownDuration(this);
        }
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        await CreatureCmd.Heal(Owner, HealAfterCombat);
    }
}
