using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Hextech;

namespace YuWanCard.Relics;

public sealed class CoinRainRune : HextechPigRuneBase
{
    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Gold;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Unpowered)];

    public override async Task AfterGoldGained(Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
        {
            return;
        }

        Flash();
        foreach (Creature enemy in Owner.Creature.CombatState.HittableEnemies)
        {
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), enemy, DynamicVars.Damage.BaseValue, ValueProp.Unpowered, Owner.Creature);
        }
    }
}
