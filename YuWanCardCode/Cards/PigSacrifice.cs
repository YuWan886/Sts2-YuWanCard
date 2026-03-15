using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigSacrifice : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public PigSacrifice() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.AnyAlly
    )
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner.Creature;
        var target = cardPlay.Target;

        int hpToTransfer = IsUpgraded ? owner.CurrentHp : owner.CurrentHp / 2;
        int blockToTransfer = IsUpgraded ? owner.Block : owner.Block / 2;

        if (hpToTransfer > 0)
        {
            await CreatureCmd.Damage(choiceContext, owner, hpToTransfer, ValueProp.Unblockable | ValueProp.Unpowered, Owner.Creature);
            await CreatureCmd.Heal(target!, hpToTransfer);
        }

        if (blockToTransfer > 0)
        {
            owner.LoseBlockInternal(blockToTransfer);
            await CreatureCmd.GainBlock(target!, blockToTransfer, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
