using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class TenYearBamboo : YuWanRelicModel
{
    [SavedProperty]
    public int BlockBonus { get; set; } = 1;

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShowCounter => true;

    public override int DisplayAmount => BlockBonus;

    public override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BlockBonus", 1m)];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }
        Flash();
        _ = await CreatureCmd.GainBlock(Owner.Creature, BlockBonus, default, null);
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        if (room == null || Owner == null)
        {
            return Task.CompletedTask;
        }
        if (room.RoomType == RoomType.Elite)
        {
            BlockBonus += 2;
            Flash();
            DynamicVars["BlockBonus"].BaseValue = BlockBonus;
            MainFile.Logger.Info($"TenYearBamboo: Elite defeated, block bonus increased to {BlockBonus}");
        }
        else if (room.RoomType == RoomType.Boss)
        {
            BlockBonus += 5;
            Flash();
            DynamicVars["BlockBonus"].BaseValue = BlockBonus;
            MainFile.Logger.Info($"TenYearBamboo: Boss defeated, block bonus increased to {BlockBonus}");
        }

        return Task.CompletedTask;
    }
}
