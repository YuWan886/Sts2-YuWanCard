using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigShieldBreak : YuWanCardModel
{
    public override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move)
    ];

    public PigShieldBreak() : base(
        baseCost: 1,
        type: CardType.Attack,
        rarity: CardRarity.Token,
        target: TargetType.AnyEnemy
    )
    {
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            int blockToRemove = cardPlay.Target.Block / 2;
            if (blockToRemove > 0)
            {
                await CreatureCmd.LoseBlock(cardPlay.Target, blockToRemove);
            }
        }
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target!)
            .Execute(choiceContext);
    }

    public override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
