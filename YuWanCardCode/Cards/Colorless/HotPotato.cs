using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Persistence;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public sealed class HotPotato : YuWanCardModel
{
    private static readonly SavedAttachedState<CardModel, int> CombatDoublingsState =
        new("HotPotatoCombatDoublings", defaultValueFactory: () => 0);

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public HotPotato() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.RandomEnemy)
    {
        WithDamage(1, 1);
    }

    protected override void OnUpgrade()
    {
        RecalculateDamage();
    }

    protected override void AfterDowngraded()
    {
        base.AfterDowngraded();
        RecalculateDamage();
    }

    protected override void AfterDeserialized()
    {
        base.AfterDeserialized();
        RecalculateDamage();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState is not { } combatState)
        {
            return;
        }

        Creature? target = cardPlay.Target;
        if (target == null || target.IsDead)
        {
            if (combatState.HittableEnemies.Count == 0)
            {
                return;
            }

            target = Owner.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies);
        }

        if (target == null)
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        CombatDoublingsState[this] += 1;
        RecalculateDamage();

        if (cardPlay.IsLastInSeries)
        {
            await GiveToRandomTeammate(combatState);
        }
    }

    private void RecalculateDamage()
    {
        DynamicVars.Damage.BaseValue = GetBaseDamage() * GetDamageMultiplier();
    }

    private int GetBaseDamage()
    {
        return CurrentUpgradeLevel > 0 ? 2 : 1;
    }

    private decimal GetDamageMultiplier()
    {
        decimal multiplier = 1;
        for (int i = 0; i < CombatDoublingsState[this]; i++)
        {
            multiplier *= 2;
        }

        return multiplier;
    }

    private async Task GiveToRandomTeammate(ICombatState combatState)
    {
        if (Owner == null)
        {
            return;
        }

        Player originalOwner = Owner;
        List<Player> teammates = combatState.GetTeammatesOf(originalOwner.Creature)
            .Where(creature => creature.IsAlive && creature.IsPlayer)
            .Select(creature => creature.Player)
            .OfType<Player>()
            .Where(player => player != originalOwner)
            .ToList();
        if (teammates.Count == 0)
        {
            return;
        }

        Player? targetPlayer = originalOwner.RunState.Rng.CombatTargets.NextItem(teammates);
        if (targetPlayer == null)
        {
            return;
        }

        CardModel? transferredCard = CardCopyHelper.CreateCombatCopy(this, targetPlayer);
        if (transferredCard == null)
        {
            return;
        }

        CombatDoublingsState[transferredCard] = CombatDoublingsState[this];
        if (transferredCard is HotPotato transferredHotPotato)
        {
            transferredHotPotato.RecalculateDamage();
        }

        await CardPileCmd.RemoveFromCombat(this);
        await CardPileCmd.AddGeneratedCardToCombat(transferredCard, PileType.Hand, originalOwner, CardPilePosition.Random);
    }
}
