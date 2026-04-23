using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using YuWanCard.Powers;

namespace YuWanCard.Cards;

[Pool(typeof(ColorlessCardPool))]
public class ShieldToFront : YuWanCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public ShieldToFront() : base(
        baseCost: 1,
        type: CardType.Skill,
        rarity: CardRarity.Uncommon,
        target: TargetType.None)
    {
        WithKeywords(CardKeyword.Exhaust);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState;
        if (combatState == null) return;

        var teammates = combatState.GetTeammatesOf(Owner.Creature)
            .Where(c => c.Player != null && c.PetOwner == null)
            .ToList();
        if (teammates.Count == 0) return;

        var lowestDamagePlayer = FindLowestDamagePlayer(teammates);

        foreach (var creature in teammates)
        {
            if (creature == lowestDamagePlayer)
            {
                await PowerCmd.Apply<ShieldToFrontPower>(creature, 1, Owner.Creature, this);
            }
            else
            {
                await PowerCmd.Apply<ShieldToFrontImmunePower>(creature, 1, Owner.Creature, this);
            }
        }
    }

    private Creature? FindLowestDamagePlayer(List<Creature> creatures)
    {
        var history = CombatManager.Instance?.History;
        if (history == null)
        {
            return creatures.FirstOrDefault();
        }

        var damageByPlayer = new Dictionary<Creature, decimal>();

        foreach (var creature in creatures)
        {
            damageByPlayer[creature] = 0m;
        }

        var damageEntries = history.Entries
            .OfType<DamageReceivedEntry>()
            .Where(e => e.HappenedThisTurn(CombatState!))
            .Where(e => e.Dealer != null && damageByPlayer.ContainsKey(e.Dealer) && e.Dealer.Player != null && e.Dealer.PetOwner == null);

        foreach (var entry in damageEntries)
        {
            if (entry.Dealer != null)
            {
                damageByPlayer[entry.Dealer] += entry.Result.UnblockedDamage;
            }
        }

        var lowestDamagePlayer = damageByPlayer
            .OrderBy(kvp => kvp.Value)
            .FirstOrDefault();

        return lowestDamagePlayer.Key;
    }
}
