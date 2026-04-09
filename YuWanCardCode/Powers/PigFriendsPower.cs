using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using YuWanCard.Monsters;

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

        var existingPig = FindExistingPig();
        
        if (existingPig != null && existingPig.IsAlive)
        {
            _summonedPig = existingPig;
            return;
        }

        Flash();
        SfxCmd.Play("event:/sfx/characters/necrobinder/necrobinder_summon");

        bool isReviving = existingPig != null && existingPig.IsDead;

        if (isReviving && existingPig != null)
        {
            _summonedPig = existingPig;
            Owner.Player?.PlayerCombatState?.AddPetInternal(existingPig);
        }
        else
        {
            _summonedPig = await PlayerCmd.AddPet<PigMinion>(Owner.Player);
        }

        int upgradeLevel = Amount / UpgradeThreshold;
        _lastUpgradeLevel = upgradeLevel;

        if (_summonedPig != null)
        {
            PositionPig(_summonedPig, upgradeLevel);
            await SetupPig(_summonedPig, upgradeLevel, isReviving);
        }

        if (isReviving && _summonedPig != null)
        {
            NCreature? pigNode = NCombatRoom.Instance?.GetCreatureNode(_summonedPig);
            if (pigNode != null)
            {
                pigNode.SetAnimationTrigger("Idle");
                pigNode.AnimEnableUi();
                pigNode.Modulate = Colors.Transparent;
                Tween tween = pigNode.CreateTween();
                tween.TweenProperty(pigNode, "modulate", Colors.White, 0.35f).SetDelay(0.1f);
            }
        }
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this) return;
        if (_summonedPig == null || _summonedPig.IsDead) return;

        int newUpgradeLevel = Amount / UpgradeThreshold;
        if (newUpgradeLevel <= _lastUpgradeLevel) return;

        int levelsGained = newUpgradeLevel - _lastUpgradeLevel;
        _lastUpgradeLevel = newUpgradeLevel;

        Flash();
        await UpgradePig(_summonedPig, levelsGained);
        PositionPig(_summonedPig, newUpgradeLevel);
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
            await CreatureCmd.Kill(_summonedPig, false);
        }
        _summonedPig = null;
    }

    private Creature? FindExistingPig()
    {
        if (CombatState == null || Owner.Player == null) return null;

        foreach (var pet in Owner.Pets)
        {
            if (pet.Monster is PigMinion)
            {
                return pet;
            }
        }
        return null;
    }

    private void PositionPig(Creature pig, int upgradeLevel)
    {
        NCreature? pigNode = NCombatRoom.Instance?.GetCreatureNode(pig);
        NCreature? ownerNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (pigNode == null || ownerNode == null) return;

        float scale = 0.5f + upgradeLevel * 0.15f;
        pigNode.SetDefaultScaleTo(scale, 0f);

        Vector2 offset = new Vector2(ownerNode.Hitbox.Size.X * 0.5f + 190f, 30f);
        pigNode.Position = ownerNode.Position + offset;

        pigNode.ToggleIsInteractable(true);
    }

    private async Task SetupPig(Creature pig, int upgradeLevel, bool isReviving = false)
    {
        int ownerMaxHp = Owner.MaxHp;
        int pigHp = ownerMaxHp / 5;
        if (pigHp < 1) pigHp = 1;

        if (isReviving)
        {
            await CreatureCmd.Heal(pig, pigHp, true);
        }
        else
        {
            await CreatureCmd.SetMaxHp(pig, pigHp);
            await CreatureCmd.Heal(pig, pigHp);
            await PowerCmd.Apply<PigMinionPower>(pig, 1, null, null);
        }

        if (upgradeLevel > 0)
        {
            await UpgradePig(pig, upgradeLevel);
        }
    }

    private async Task UpgradePig(Creature pig, int levels)
    {
        int bonusHp = levels * 5;
        int bonusStrength = levels;

        await CreatureCmd.GainMaxHp(pig, bonusHp);
        await CreatureCmd.Heal(pig, bonusHp);

        if (bonusStrength > 0)
        {
            await PowerCmd.Apply<StrengthPower>(pig, bonusStrength, Owner, null);
        }
    }
}
