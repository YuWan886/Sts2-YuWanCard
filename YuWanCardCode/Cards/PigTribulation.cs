using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using YuWanCard.Characters;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class PigTribulation : YuWanCardModel
{
    public PigTribulation() : base(
        baseCost: 0,
        type: CardType.Attack,
        rarity: CardRarity.Common,
        target: TargetType.AllEnemies)
    {
        WithDamage(3);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var attackCmd = CommonActions.CardAttack(this, cardPlay);
        attackCmd.WithHitFx("vfx/vfx_attack_lightning", null, "lightning_orb_evoke.mp3");
        await attackCmd.Execute(choiceContext);
    }
}
