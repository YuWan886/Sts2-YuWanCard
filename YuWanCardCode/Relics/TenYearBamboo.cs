using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Relics;

[Pool(typeof(SharedRelicPool))]
public class TenYearBamboo : YuWanRelicModel
{
    private int _blockBonus = 1;

    [SavedProperty]
    public int YuWanCard_BlockBonus
    {
        get => _blockBonus;
        set
        {
            _blockBonus = value;
            DynamicVars["BlockBonus"].BaseValue = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShowCounter => true;

    public override int DisplayAmount => YuWanCard_BlockBonus;

    public override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("BlockBonus", 1m)];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }
        Flash();
        _ = await CreatureCmd.GainBlock(Owner.Creature, YuWanCard_BlockBonus, default, null);
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        if (room == null || Owner == null)
        {
            return Task.CompletedTask;
        }
        if (room.RoomType == RoomType.Elite)
        {
            YuWanCard_BlockBonus += 1;
            Flash();
            MainFile.Logger.Info($"TenYearBamboo: Elite defeated, block bonus increased to {YuWanCard_BlockBonus}");
        }
        else if (room.RoomType == RoomType.Boss)
        {
            YuWanCard_BlockBonus += 3;
            Flash();
            MainFile.Logger.Info($"TenYearBamboo: Boss defeated, block bonus increased to {YuWanCard_BlockBonus}");
        }

        return Task.CompletedTask;
    }
}
