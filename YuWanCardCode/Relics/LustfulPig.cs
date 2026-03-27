using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public class LustfulPig : YuWanRelicModel
{
    [SavedProperty]
    private bool HasAppliedDebuffsThisCombat { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public LustfulPig() : base(true)
    {
    }

    public override RelicModel? GetUpgradeReplacement() => null;

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
        {
            return Task.CompletedTask;
        }

        HasAppliedDebuffsThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }

        if (HasAppliedDebuffsThisCombat || Owner?.Creature?.CombatState == null)
        {
            return;
        }

        HasAppliedDebuffsThisCombat = true;
        Flash();

        // 给所有敌人施加 2 层虚弱
        var combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        foreach (var enemy in combatState.Enemies)
        {
            await PowerCmd.Apply<WeakPower>(enemy, 2, Owner.Creature, null);
        }

        MainFile.Logger.Info($"LustfulPig: Applied 2 Weak to all enemies");
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        await base.AfterCombatVictory(room);
        HasAppliedDebuffsThisCombat = false;
    }
}
