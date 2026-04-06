using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigBlessing : YuWanCardModel
{
    public PigBlessing() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.AllAllies)
    {
        WithPower<StrengthPower>(1);
        WithPower<DexterityPower>(1);
        WithPower<RegenPower>(2);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Strength.UpgradeValueBy(1);
        DynamicVars["DexterityPower"].UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var teammates = CombatState!.GetTeammatesOf(Owner.Creature);
        foreach (var teammate in teammates)
        {
            await PowerCmd.Apply<StrengthPower>(teammate, DynamicVars.Strength.IntValue, Owner.Creature, this);
            await PowerCmd.Apply<DexterityPower>(teammate, DynamicVars["DexterityPower"].IntValue, Owner.Creature, this);
            await PowerCmd.Apply<RegenPower>(teammate, DynamicVars["RegenPower"].IntValue, Owner.Creature, this);
        }
    }
}
