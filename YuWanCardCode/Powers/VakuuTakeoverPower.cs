using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;

namespace YuWanCard.Powers;

public class VakuuTakeoverPower : YuWanPowerModel
{
    private const int MaxCardsToPlay = 114514;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Duration", 1m)];

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        var combatState = player.Creature.CombatState;
        if (combatState == null) return;

        Flash();

        using (CardSelectCmd.PushSelector(new VakuuCardSelector()))
        {
            int cardsPlayed;
            for (cardsPlayed = 0; cardsPlayed < MaxCardsToPlay; cardsPlayed++)
            {
                if (CombatManager.Instance.IsOverOrEnding) break;
                if (CombatManager.Instance.IsPlayerReadyToEndTurn(player)) break;

                var pile = PileType.Hand.GetPile(player);
                var card = pile.Cards.FirstOrDefault(c => c.CanPlay() && !RequiresPlayerChoice(c));
                if (card == null) break;

                var target = GetTarget(card, combatState);
                await card.SpendResources();
                await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default, skipXCapture: true);
            }

            if (cardsPlayed > 0)
            {
                await PowerCmd.Decrement(this);
            }
        }
    }

    private static bool RequiresPlayerChoice(CardModel card)
    {
        return card is Discovery;
    }

    private Creature? GetTarget(CardModel card, ICombatState combatState)
    {
        var rng = Owner.Player?.RunState.Rng;
        if (rng == null) return null;

        return card.TargetType switch
        {
            TargetType.AnyEnemy => combatState.HittableEnemies.FirstOrDefault(),
            TargetType.AnyAlly => rng.CombatTargets.NextItem(combatState.Allies.Where(c => c != null && c.IsAlive && c.IsPlayer && c != Owner)),
            TargetType.AnyPlayer => Owner,
            _ => null
        };
    }
}
