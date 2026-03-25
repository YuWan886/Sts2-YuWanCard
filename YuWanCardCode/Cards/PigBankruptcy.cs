using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigBankruptcy : YuWanCardModel
{
    public PigBankruptcy() : base(
        baseCost: 3,
        type: CardType.Skill,
        rarity: CardRarity.Rare,
        target: TargetType.Self)
    {
        WithTip(CardKeyword.Exhaust);
    }

    public override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        NPowerUpVfx.CreateNormal(Owner.Creature);
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<PigBankruptcyPower>(Owner.Creature, 1, Owner.Creature, this);
    }
}
