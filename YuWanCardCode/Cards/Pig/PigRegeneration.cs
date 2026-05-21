using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigRegeneration : YuWanCardModel
{
    public PigRegeneration() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithVar("HealPercent", 50);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["HealPercent"].UpgradeValueBy(25);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var damageTakenThisCombat = Owner.RunState.CurrentMapPointHistoryEntry?.GetEntry(Owner.NetId).DamageTaken ?? 0;
        if (damageTakenThisCombat <= 0)
            return;

        var healAmount = damageTakenThisCombat * DynamicVars["HealPercent"].BaseValue / 100m;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await CreatureCmd.Heal(Owner.Creature, healAmount);
    }
}
