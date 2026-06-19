using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Powers.MaliceTraits;

public sealed class CataclysmTrait : MaliceTraitPowerBase
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("CataclysmDamage", 5m)];
    protected override string[] AutoUpdateVarNames => ["CataclysmDamage"];

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side || Owner.IsDead)
        {
            return;
        }

        int actIndex = combatState.RunState?.CurrentActIndex ?? 0;
        int damage = (5 + 2 * Math.Min(Math.Max(actIndex, 0), 2)) * (int)Amount;

        bool flashed = false;
        var context = new ThrowingPlayerChoiceContext();
        foreach (var player in combatState.Players)
        {
            if (player.Creature.IsDead)
            {
                continue;
            }

            if (!flashed)
            {
                Flash();
                flashed = true;
            }

            await CreatureCmd.Damage(context, player.Creature, damage, ValueProp.Unpowered, Owner, null);
        }
    }
}
