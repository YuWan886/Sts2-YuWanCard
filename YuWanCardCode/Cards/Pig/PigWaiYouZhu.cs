using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigWaiYouZhu : YuWanCardModel
{
    public PigWaiYouZhu() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Common,
        target: TargetType.Self)
    {
        WithBlock(15);
        WithPower<PigFriendsPower>(3);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(5);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        if (CardsPlayedThisTurn < 3)
        {
            await PowerCmd.Apply<PigFriendsPower>(Owner.Creature, DynamicVars["PigFriendsPower"].IntValue, Owner.Creature, this);
        }
    }

    private int CardsPlayedThisTurn =>
        CombatState == null
            ? 0
            : CombatManager.Instance.History.CardPlaysStarted.Count(e =>
                e.HappenedThisTurn(CombatState) &&
                e.CardPlay.Card.Owner == Owner);
}
