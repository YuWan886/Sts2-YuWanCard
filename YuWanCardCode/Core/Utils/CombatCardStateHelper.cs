using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Core.Utils;

internal static class CombatCardStateHelper
{
    public static List<CardModel> EnsureRegistered(IEnumerable<CardModel> cards, string sourceTag)
    {
        List<CardModel> cardList = cards.ToList();
        foreach (CardModel card in cardList)
        {
            if (card.Owner?.Creature?.CombatState is not { } combatState)
            {
                continue;
            }

            if (!card.IsInCombat || combatState.ContainsCard(card))
            {
                continue;
            }

            combatState.AddCard(card, card.Owner);
            MainFile.Logger.Warn(
                $"[{sourceTag}] Re-registered combat card {card.Id.Entry} before moving it between combat piles.");
        }

        return cardList;
    }
}
