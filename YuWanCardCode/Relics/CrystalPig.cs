using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Core.Abstracts;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class CrystalPig : YuWanRelicModel
{
    private bool _shouldTriggerOnEnergyGain;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Damage", 2m),
        new DynamicVar("Block", 1m)
    ];

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public CrystalPig() : base(true)
    {
    }

    public override decimal ModifyEnergyGain(Player player, decimal amount)
    {
        if (player == Owner && amount > 0)
        {
            _shouldTriggerOnEnergyGain = true;
        }

        return amount;
    }

    public override async Task AfterModifyingEnergyGain()
    {
        if (!_shouldTriggerOnEnergyGain)
        {
            return;
        }

        _shouldTriggerOnEnergyGain = false;
        await TriggerCrystalPulse(new ThrowingPlayerChoiceContext());
    }

    public override async Task AfterEnergyReset(Player player)
    {
        if (player == Owner)
        {
            await TriggerCrystalPulse(new ThrowingPlayerChoiceContext());
        }
    }

    private async Task TriggerCrystalPulse(PlayerChoiceContext choiceContext)
    {
        if (Owner?.Creature == null)
        {
            return;
        }

        Creature? target = CombatTargetingUtils.GetDeterministicRandomLivingEnemy(Owner);

        if (target == null)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(choiceContext, target, DynamicVars["Damage"].BaseValue, ValueProp.Unpowered, Owner.Creature, null);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars["Block"].BaseValue, ValueProp.Unpowered, null);
    }
}
