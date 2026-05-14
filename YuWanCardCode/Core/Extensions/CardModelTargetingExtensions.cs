using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Extensions;

public static class CardModelTargetingExtensions
{
    public static List<Creature> GetSelectableTargets(this CardModel card)
    {
        ArgumentNullException.ThrowIfNull(card);

        var state = card.CombatState;
        if (state == null)
            return [];

        return card.TargetType switch
        {
            TargetType.AnyEnemy or TargetType.AllEnemies or TargetType.RandomEnemy
                => state.HittableEnemies.ToList(),
            TargetType.AnyAlly or TargetType.AllAllies
                => state.Allies.Where(c => c != null && c.IsAlive).ToList(),
            TargetType.AnyPlayer
                => state.Players.Where(p => p?.Creature is { IsAlive: true }).Select(p => p.Creature).ToList(),
            TargetType.None => [],
            TargetType.Self => [card.Owner.Creature],
            _ => GetCustomSelectableTargets(card, state)
        };
    }

    public static Creature? PickRandomTarget(this CardModel card)
    {
        var candidates = card.GetSelectableTargets();
        if (candidates.Count == 0)
            return null;

        return card.Owner.RunState.Rng.CombatTargets.NextItem(candidates);
    }

    private static List<Creature> GetCustomSelectableTargets(CardModel card, CombatState state)
    {
        if (CustomTargetType.IsCustomSingleTargetType(card.TargetType))
        {
            return state.Creatures
                .Where(c =>
                    CustomTargetTypeRegistry.TryIsAllowedSingleTarget(card.TargetType, c, out var allowed) && allowed)
                .ToList();
        }

        if (!CustomTargetType.IsCustomMultiTargetType(card.TargetType))
            return [];

        return state.Creatures
            .Where(c =>
                CustomTargetTypeRegistry.TryShouldIncludeMultiTarget(card.TargetType, c, out var include) && include)
            .ToList();
    }
}
