using YuWanCard.Core.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Characters;
using YuWanCard.Config;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(PigCardPool))]
public class BianBianPig : YuWanCardModel
{
    public BianBianPig() : base(
        baseCost: 2,
        type: CardType.Attack,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyEnemy)
    {
        WithDamage(14);
        WithCostUpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        if (CombatState == null || Owner == null)
        {
            return;
        }

        var colorlessCards = YuWanColorlessCardCatalog.GetUnlockedDoctorPigCards(Owner.RunState).ToList();
        if (colorlessCards.Count == 0)
        {
            return;
        }

        CardModel? randomCard = DeterministicRandomUtils.PickStableRandom(
            colorlessCards,
            Owner.RunState.Rng.CombatCardGeneration);

        if (randomCard == null)
        {
            return;
        }

        CardModel combatCard = CombatState.CreateCard(randomCard, Owner);
        if (IsUpgraded && combatCard.IsUpgradable)
        {
            CardCmd.Upgrade(combatCard);
        }

        await CardPileCmd.AddGeneratedCardToCombat(combatCard, PileType.Hand, Owner);
    }
}
