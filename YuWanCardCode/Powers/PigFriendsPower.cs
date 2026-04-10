using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Monsters;
using YuWanCard.Utils;

namespace YuWanCard.Powers;

public class PigFriendsPower : YuWanPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("PigFriendsPower", 1),
        new DynamicVar("UpgradeThreshold", 2)
    ];

    public int UpgradeThreshold => DynamicVars["UpgradeThreshold"].IntValue;

    private Creature? _summonedPig;
    private int _lastUpgradeLevel;
    internal bool _isBeingRemoved = false;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner.Player == null) return;

        await SummonPig();
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this) return;
        
        if (_summonedPig == null || _summonedPig.IsDead)
        {
            await SummonPig();
            return;
        }

        int newUpgradeLevel = Amount / UpgradeThreshold;
        if (newUpgradeLevel <= _lastUpgradeLevel) return;

        int levelsGained = newUpgradeLevel - _lastUpgradeLevel;
        _lastUpgradeLevel = newUpgradeLevel;

        Flash();
        await PetManager.UpgradePigMinion(_summonedPig, levelsGained, Owner);
        PetManager.PositionPet(Owner, _summonedPig, newUpgradeLevel);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_isBeingRemoved) return;
        _isBeingRemoved = true;

        if (_summonedPig != null && !_summonedPig.IsDead)
        {
            var pigMinionPower = _summonedPig.GetPower<PigMinionPower>();
            if (pigMinionPower != null)
            {
                pigMinionPower._isBeingRemoved = true;
            }
            await PetManager.KillPet(_summonedPig, false);
        }
        _summonedPig = null;
    }

    private async Task SummonPig()
    {
        if (Owner.Player == null) return;

        var existingPig = PetManager.FindPetByType<PigMinion>(Owner);
        
        if (existingPig != null && existingPig.IsAlive)
        {
            _summonedPig = existingPig;
            return;
        }

        Flash();

        int upgradeLevel = Amount / UpgradeThreshold;
        _lastUpgradeLevel = upgradeLevel;

        _summonedPig = await PetManager.SummonPigMinion(Owner.Player, upgradeLevel);
    }
}
