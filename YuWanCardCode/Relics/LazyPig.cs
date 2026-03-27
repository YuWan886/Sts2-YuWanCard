using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public class LazyPig : YuWanRelicModel
{
    [SavedProperty]
    private int _turnCount;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress;

    public override int DisplayAmount => _turnCount;

    public override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new DynamicVar("Turns", 5m)
    ];

    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this), HoverTipFactory.FromPower<RingingPower>()];

    public LazyPig() : base(true)
    {
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
        {
            return;
        }
        _turnCount++;
        InvokeDisplayAmountChanged();
        Flash();
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, player);
        if (_turnCount >= DynamicVars["Turns"].IntValue)
        {
            await PowerCmd.Apply<RingingPower>(player.Creature, 1, player.Creature, null);
        }
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
        {
            return Task.CompletedTask;
        }
        _turnCount = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _turnCount = 0;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}
