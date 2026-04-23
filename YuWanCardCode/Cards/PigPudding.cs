using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigPudding : YuWanCardModel
{
    public PigPudding() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Token,
        target: TargetType.Self)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var debuffs = Owner.Creature.Powers
            .Where(p => p.Type == PowerType.Debuff)
            .ToList();

        if (debuffs.Count == 0) return;

        var randomDebuff = Owner.RunState.Rng.CombatCardGeneration.NextItem(debuffs);
        await PowerCmd.Remove(randomDebuff);
        
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
    }
}
