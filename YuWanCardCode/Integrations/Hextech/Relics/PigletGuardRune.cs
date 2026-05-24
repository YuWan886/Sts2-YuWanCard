using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using YuWanCard.Hextech;
using YuWanCard.Powers;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

public sealed class PigletGuardRune : HextechPigRuneBase
{
    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Silver;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PlatingPower>(3m),
        new BlockVar(2m, ValueProp.Unpowered)
    ];

    public override async Task BeforeCombatStart()
    {
        if (Owner == null)
        {
            return;
        }

        Creature? pig = PetManager.FindPetByType<Monsters.PigMinion>(Owner.Creature);
        if (pig == null)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<HextechPigletGuardMinionPower>(pig, 1, Owner.Creature, null);
        await PowerCmd.Apply<PlatingPower>(pig, DynamicVars["PlatingPower"].BaseValue, Owner.Creature, null);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player != Owner)
        {
            return;
        }

        Creature? pig = PetManager.FindPetByType<Monsters.PigMinion>(Owner.Creature);
        if (pig == null || pig.IsDead)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Unpowered, null);
    }
}
