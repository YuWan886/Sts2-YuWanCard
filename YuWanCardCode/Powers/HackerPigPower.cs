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
    private static readonly SpireField<HackerPigPower, List<CardModel>> MarkedCardsField = new(_ => new List<CardModel>());

    private List<CardModel> MarkedCards => MarkedCardsField[this] ?? new List<CardModel>();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("HackerPigPower", 1m)];

    public static void MarkCard(CardModel card, HackerPigPower power)
    {
        power.MarkedCards.Add(card);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;
        if (MarkedCards.Count == 0) return;

        Flash();

        var cardsToPlay = MarkedCards.Where(c => c.Pile != null).ToList();
        MarkedCards.Clear();

        foreach (var card in cardsToPlay)
        {
            if (CombatManager.Instance.IsOverOrEnding) break;

            var currentPile = card.Pile?.Type;
            if (currentPile != PileType.Hand)
            {
                await CardPileCmd.Add(card, PileType.Hand);
            }

            Creature? target = GetTargetForCard(card, player.Creature.CombatState);
            await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default, skipXCapture: true);
        }
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