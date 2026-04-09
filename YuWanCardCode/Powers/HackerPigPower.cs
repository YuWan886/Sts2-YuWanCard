using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Powers;

public class HackerPigPower : YuWanPowerModel
{
    private static readonly SpireField<CardModel, bool> HackerPigMarkedCards = new(_ => false);

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("HackerPigPower", 1m)];

    public static void MarkCard(CardModel card)
    {
        HackerPigMarkedCards[card] = true;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        var allCards = PileType.Draw.GetPile(player).Cards
            .Concat(PileType.Hand.GetPile(player).Cards)
            .Concat(PileType.Discard.GetPile(player).Cards)
            .ToList();

        var markedCards = allCards.Where(c => HackerPigMarkedCards[c]).ToList();
        if (markedCards.Count == 0) return;

        Flash();

        foreach (var card in markedCards)
        {
            if (CombatManager.Instance.IsOverOrEnding) break;

            var currentPile = card.Pile?.Type;
            if (currentPile != PileType.Hand)
            {
                await CardPileCmd.Add(card, PileType.Hand);
            }

            Creature? target = GetTargetForCard(card, player.Creature.CombatState);
            await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default, skipXCapture: true);
            HackerPigMarkedCards[card] = false;
        }

        await PowerCmd.Decrement(this);
    }

    private Creature? GetTargetForCard(CardModel card, CombatState? combatState)
    {
        if (combatState == null) return null;

        var rng = Owner.Player?.RunState.Rng;
        if (rng == null) return null;

        return card.TargetType switch
        {
            TargetType.AnyEnemy or TargetType.AllEnemies or TargetType.RandomEnemy
                => rng.CombatTargets.NextItem(combatState.HittableEnemies),
            TargetType.AnyPlayer
                => rng.CombatTargets.NextItem(combatState.Players.Where(p => p.Creature.IsAlive).Select(p => p.Creature)),
            TargetType.AnyAlly or TargetType.AllAllies
                => rng.CombatTargets.NextItem(combatState.Allies.Where(c => c != null && c.IsAlive)),
            TargetType.Self => Owner,
            _ => null
        };
    }
}