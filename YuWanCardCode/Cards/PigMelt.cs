using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigMelt : YuWanCardModel
{
    public PigMelt() : base(
        baseCost: 0,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.Self)
    {
        WithPower<BufferPower>(1);
        WithKeywords(CardKeyword.Exhaust);
    }

    public override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<BufferPower>(Owner.Creature, DynamicVars["BufferPower"].BaseValue, Owner.Creature, this);
        PlayerCmd.EndTurn(Owner, canBackOut: false);
    }
}
