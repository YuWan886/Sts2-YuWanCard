using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Hextech;
using YuWanCard.Utils;

namespace YuWanCard.Relics;

public sealed class GluttonsFeastRune : HextechPigRuneBase
{
    public override HextechRuneRarity HextechRarity => HextechRuneRarity.Silver;

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature?.CombatState == null)
        {
            return;
        }

        CardModel card = CardUtils.CreateRandomFoodPigCard(Owner);
        Flash();
        await CardPileCmd.AddGeneratedCardsToCombat([card], PileType.Hand, addedByPlayer: true);
    }
}
