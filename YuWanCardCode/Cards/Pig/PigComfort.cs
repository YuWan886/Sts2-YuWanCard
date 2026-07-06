using YuWanCard.Core.Abstracts;
using YuWanCard.Core;
using YuWanCard.Core.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigComfort : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public PigComfort() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: CustomTargetType.AllPlayers)
    {
        WithCards(1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
        {
            return;
        }

        foreach (var creature in CombatState.GetLivingPlayerCreatures())
        {
            var debuff = DeterministicRandomUtils.PickStableRandom(
                creature.Powers.Where(p => p.Type == PowerType.Debuff),
                Owner.RunState.Rng.CombatCardGeneration);
            if (debuff != null)
            {
                await PowerCmd.Remove(debuff);
            }
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
    }
}
