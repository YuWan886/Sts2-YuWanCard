using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Core;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class PigGaze : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public PigGaze() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: CustomTargetType.AnyOtherPlayer)
    {
        WithVars(new EnergyVar(2));
        WithPower<NoDrawPower>(1);
        WithEnergyTip();
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var targetPlayer = cardPlay.Target?.Player;
        if (targetPlayer == null || targetPlayer == Owner)
        {
            return;
        }

        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, targetPlayer);
        await PowerCmd.Apply<NoDrawPower>(
            new ThrowingPlayerChoiceContext(),
            targetPlayer.Creature,
            DynamicVars["NoDrawPower"].BaseValue,
            Owner.Creature,
            this);
    }
}
