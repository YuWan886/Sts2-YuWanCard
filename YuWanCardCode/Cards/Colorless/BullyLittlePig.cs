using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class BullyLittlePig : YuWanCardModel
{
    public BullyLittlePig() : base(
        baseCost: 3,
        type: CardType.Power,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithPower<BullyLittlePigSkittishPower>(9);
        WithPower<HardToKillPower>(9);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<BullyLittlePigSkittishPower>(Owner.Creature, DynamicVars["BullyLittlePigSkittishPower"].BaseValue, Owner.Creature, this);
        await PowerCmd.Apply<HardToKillPower>(Owner.Creature, DynamicVars["HardToKillPower"].BaseValue, Owner.Creature, this);
    }
}
