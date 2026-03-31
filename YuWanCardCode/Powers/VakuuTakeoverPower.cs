using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Powers;

public class VakuuTakeoverPower : YuWanPowerModel
{
    private const int MaxCardsToPlay = 114514;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != CombatSide.Player) return;
        if (Owner.Side != CombatSide.Player) return;

        Flash();

        var player = Owner.Player;
        if (player == null) return;

        int cardsPlayed = 0;
        var pile = PileType.Hand.GetPile(player);

        while (cardsPlayed < MaxCardsToPlay)
        {
            if (CombatManager.Instance.IsOverOrEnding) break;

            var card = pile.Cards.FirstOrDefault(c => c.CanPlay());
            if (card == null) break;

            var target = GetTarget(card, combatState);
            await card.SpendResources();
            await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(), card, target, AutoPlayType.Default, skipXCapture: true);
            cardsPlayed++;
        }

        await PowerCmd.Decrement(this);
    }

    private Creature? GetTarget(CardModel card, CombatState combatState)
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
