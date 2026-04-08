using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YuWanCard.Characters;
using YuWanCard.Monsters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigTouch : YuWanCardModel
{
    public PigTouch() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.None)
    {
        WithVars(new HealVar(5));
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Heal.UpgradeValueBy(2);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pig = FindPigPet();
        if (pig != null && !pig.IsDead)
        {
            await CreatureCmd.Heal(pig, DynamicVars.Heal.BaseValue);
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