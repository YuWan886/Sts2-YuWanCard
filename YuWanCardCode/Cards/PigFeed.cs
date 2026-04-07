using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Monsters;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigFeed : YuWanCardModel
{
    public PigFeed() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Token,
        target: TargetType.Self)
    {
        WithVars(new EnergyVar(1));
        WithCards(1);
        WithVar("Heal", 1);
        WithKeywords(CardKeyword.Exhaust);
        WithEnergyTip();
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);

        var pig = FindPigPet();
        if (pig != null && !pig.IsDead)
        {
            await CreatureCmd.Heal(pig, DynamicVars["Heal"].BaseValue);
        }
    }

    private Creature? FindPigPet()
    {
        if (Owner == null) return null;

        foreach (var pet in Owner.Creature.Pets)
        {
            if (pet.Monster is PigMinion)
            {
                return pet;
            }
        }
        return null;
    }
}
