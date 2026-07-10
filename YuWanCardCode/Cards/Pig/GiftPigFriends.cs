using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;
using YuWanCard.Powers;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class GiftPigFriends : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public GiftPigFriends() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AllAllies)
    {
        WithPower<PigFriendsPower>(1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PigFriendsPower"].UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        foreach (var creature in CombatState!.GetLivingPlayerCreatures())
        {
            await PowerCmd.Apply<PigFriendsPower>(new ThrowingPlayerChoiceContext(), creature, DynamicVars["PigFriendsPower"].IntValue, Owner.Creature, this);
        }

        VfxUtils.PlayStaticVfxAtCreatureTop(Owner.Creature);
    }
}
