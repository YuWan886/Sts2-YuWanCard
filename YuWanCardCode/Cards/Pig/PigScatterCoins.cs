using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using YuWanCard.Characters;
using YuWanCard.Core.Abstracts;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public sealed class PigScatterCoins : YuWanCardModel
{
    protected override bool IsPlayable =>
        base.IsPlayable && GoldSpendHelper.CanAfford(Owner, DynamicVars.Gold.IntValue);

    protected override bool ShouldGlowRedInHand =>
        Owner != null && Owner.Gold < DynamicVars.Gold.IntValue;

    public PigScatterCoins() : base(
        baseCost: 0,
        type: CardType.Attack,
        rarity: CardRarity.Common,
        target: TargetType.AnyEnemy)
    {
        WithVars(new GoldVar(6));
        WithDamage(10);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
        {
            return;
        }

        if (!await GoldSpendHelper.TrySpend(Owner, DynamicVars.Gold.IntValue, nameof(PigScatterCoins)))
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
}
