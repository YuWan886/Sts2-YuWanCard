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

    public PigSacrifice() : base(
        baseCost: 2,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.AnyAlly)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
        RemoveKeyword(CardKeyword.Exhaust);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner.Creature;
        var targetCreature = cardPlay.Target;

        int hpToTransfer = IsUpgraded ? owner.CurrentHp : owner.CurrentHp / 2;
        int blockToTransfer = IsUpgraded ? owner.Block : owner.Block / 2;

        if (hpToTransfer > 0)
        {
            await CreatureCmd.Damage(choiceContext, owner, hpToTransfer, ValueProp.Unblockable | ValueProp.Unpowered, Owner.Creature);
            await CreatureCmd.Heal(targetCreature!, hpToTransfer);
        }

        if (blockToTransfer > 0)
        {
            owner.LoseBlockInternal(blockToTransfer);
            await CreatureCmd.GainBlock(targetCreature!, blockToTransfer, ValueProp.Move, null);
        }
    }
}
