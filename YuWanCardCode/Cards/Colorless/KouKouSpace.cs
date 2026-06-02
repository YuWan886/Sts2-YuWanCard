using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using YuWanCard.Core.Abstracts;
using YuWanCard.Utils;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class KouKouSpace : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public KouKouSpace() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.AnyAlly)
    {
        WithPower<WeakPower>(1);
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
        {
            return;
        }

        var aliveTeammates = CombatState!.GetTeammatesOf(Owner.Creature)
            .Where(teammate => teammate.IsAlive && teammate.Player != null)
            .ToList();

        if (IsUpgraded)
        {
            foreach (var teammate in aliveTeammates)
            {
                await PowerCmd.Apply<WeakPower>(teammate, 1, Owner.Creature, this);
            }

            await GainRandomPotions(aliveTeammates.Count);
            return;
        }

        if (cardPlay.Target?.Player == null || cardPlay.Target.Player == Owner)
        {
            return;
        }

        await PowerCmd.Apply<WeakPower>(cardPlay.Target, 1, Owner.Creature, this);
        await GainRandomPotions(1);
    }

    private async Task GainRandomPotions(int count)
    {
        if (Owner == null || count <= 0)
        {
            return;
        }

        var potions = PotionFactory.CreateRandomPotionsOutOfCombat(
            Owner,
            count,
            Owner.RunState.Rng.CombatPotionGeneration);

        foreach (var potion in potions)
        {
            if (!Owner.HasOpenPotionSlots)
            {
                break;
            }

            await PotionCmd.TryToProcure(potion.ToMutable(), Owner);
        }

        VfxUtils.PlayStaticVfxAtCreatureTop(Owner.Creature);
    }
}
