using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class RainDarkPower : YuWanPowerModel
{
    private const int HealAfterCombat = 6;
    private Player? _subscribedPlayer;
    private readonly RecursiveCallGuard<int> _energyGuard;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public RainDarkPower()
    {
        _energyGuard = new RecursiveCallGuard<int>(
            action: ExecuteEnergyGain,
            shouldProcess: gained => gained > 0
        );
    }

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
        int gained = newEnergy - oldEnergy;

        if (_energyGuard.TryExecute(gained))
        {
            return;
        }

        _ = Task.Run(async () => await _energyGuard.ExecutePendingAsync());
    }

    private async Task ExecuteEnergyGain(int gained)
    {
        if (_subscribedPlayer != null && _subscribedPlayer.PlayerCombatState != null)
        {
            _subscribedPlayer.PlayerCombatState.GainEnergy(gained);
        }
        await Task.CompletedTask;
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
