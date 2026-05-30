using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Hextech;
using YuWanCard.Monsters;

namespace YuWanCard.Relics;

public sealed class ThroneOfPigsRune : HextechPigRuneBase
{
    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Prismatic;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (Owner == null || power.Owner != Owner.Creature || power is not StrengthPower || amount <= 0)
        {
            return;
        }

        foreach (Creature pet in Owner.Creature.Pets.Where(pet => pet.Monster is PigMinion && !pet.IsDead))
        {
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), pet, amount, Owner.Creature, cardSource);
        }
    }
}
