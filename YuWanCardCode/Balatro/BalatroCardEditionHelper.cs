using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Core.Abstracts;
using YuWanCard.Core.Utils;
using YuWanCard.Relics;

namespace YuWanCard.Balatro;

public static class BalatroCardEditionHelper
{
    public const string GenericEditionSavedPropertyName = "YUWANCARD_GenericEdition";
    public const string GenericFoilAppliedSavedPropertyName = "YUWANCARD_GenericFoilApplied";

    private static readonly SpireField<CardModel, int> GenericEdition = new(() => (int)BalatroCardEdition.None);
    private static readonly SpireField<CardModel, bool> GenericFoilApplied = new(() => false);

    public static bool CanApplyEdition(CardModel? card, BalatroCardEdition edition)
    {
        if (edition == BalatroCardEdition.None || card == null)
        {
            return false;
        }

        if (card is YuWanCardModel mutable)
        {
            return mutable.CanApplyBalatroEdition(edition);
        }

        if (GetEdition(card) != BalatroCardEdition.None)
        {
            return false;
        }

        return card.Type is not CardType.None and not CardType.Status and not CardType.Curse and not CardType.Quest;
    }

    public static bool TryApplyEdition(CardModel? card, BalatroCardEdition edition)
    {
        if (edition == BalatroCardEdition.None || card == null)
        {
            return false;
        }

        CardModel? deckVersion = card.DeckVersion;
        bool appliedToCard = TryApplyEditionToSingleCard(card, edition);
        bool appliedToDeckVersion = deckVersion != null
            && !ReferenceEquals(deckVersion, card)
            && TryApplyEditionToSingleCard(deckVersion, edition);

        if (!appliedToCard && !appliedToDeckVersion)
        {
            return false;
        }

        if (card.Owner?.GetRelic<GrowingJoker>() != null)
        {
            _ = CreatureCmd.GainMaxHp(card.Owner.Creature, 3);
        }

        return true;
    }

    public static BalatroCardEdition GetEdition(CardModel? card)
    {
        if (card == null)
        {
            return BalatroCardEdition.None;
        }

        if (card is YuWanCardModel yuWanCard)
        {
            return yuWanCard.BalatroEdition;
        }

        int storedValue = GenericEdition[card];
        if (!Enum.IsDefined(typeof(BalatroCardEdition), storedValue) || storedValue == (int)BalatroCardEdition.None)
        {
            return InferEditionFromKeywords(card);
        }

        return (BalatroCardEdition)storedValue;
    }

    public static void WriteGenericEditionToSerializable(CardModel card, SerializableCard save)
    {
        if (card is YuWanCardModel)
        {
            return;
        }

        BalatroCardEdition edition = GetEdition(card);
        if (edition == BalatroCardEdition.None)
        {
            return;
        }

        save.Props ??= new SavedProperties();
        save.Props.ints ??= [];
        Upsert(save.Props.ints, GenericEditionSavedPropertyName, (int)edition);

        save.Props.bools ??= [];
        Upsert(save.Props.bools, GenericFoilAppliedSavedPropertyName, GenericFoilApplied[card]);
    }

    public static void RestoreGenericEditionFromSerializable(CardModel card, SerializableCard save)
    {
        if (card is YuWanCardModel || save.Props == null)
        {
            return;
        }

        BalatroCardEdition edition = GetSavedEdition(save.Props);
        if (edition == BalatroCardEdition.None)
        {
            return;
        }

        GenericEdition[card] = (int)edition;
        bool foilApplied = GetSavedFoilApplied(save.Props);
        GenericFoilApplied[card] = foilApplied;

        CardKeyword keyword = GetEditionKeyword(edition);
        if (keyword != CardKeyword.None && !card.Keywords.Contains(keyword))
        {
            card.AddKeyword(keyword);
        }

        if (edition == BalatroCardEdition.Foil && !foilApplied)
        {
            ApplyFoilEdition(card);
            GenericFoilApplied[card] = true;
        }
    }

    private static BalatroCardEdition InferEditionFromKeywords(CardModel card)
    {
        if (card.Keywords.Contains(BalatroCardKeywords.Foil))
        {
            return BalatroCardEdition.Foil;
        }

        if (card.Keywords.Contains(BalatroCardKeywords.Holographic))
        {
            return BalatroCardEdition.Holographic;
        }

        if (card.Keywords.Contains(BalatroCardKeywords.Polychrome))
        {
            return BalatroCardEdition.Polychrome;
        }

        if (card.Keywords.Contains(BalatroCardKeywords.Negative))
        {
            return BalatroCardEdition.Negative;
        }

        return BalatroCardEdition.None;
    }

    private static BalatroCardEdition GetSavedEdition(SavedProperties props)
    {
        int storedValue = props.ints?.FirstOrDefault(prop => prop.name == GenericEditionSavedPropertyName).value
            ?? (int)BalatroCardEdition.None;
        return Enum.IsDefined(typeof(BalatroCardEdition), storedValue)
            ? (BalatroCardEdition)storedValue
            : BalatroCardEdition.None;
    }

    private static bool GetSavedFoilApplied(SavedProperties props)
    {
        return props.bools?.FirstOrDefault(prop => prop.name == GenericFoilAppliedSavedPropertyName).value ?? false;
    }

    public static bool HasEdition(CardModel? card)
    {
        return GetEdition(card) != BalatroCardEdition.None;
    }

    public static CardKeyword GetEditionKeyword(BalatroCardEdition edition)
    {
        return edition switch
        {
            BalatroCardEdition.Foil => BalatroCardKeywords.Foil,
            BalatroCardEdition.Holographic => BalatroCardKeywords.Holographic,
            BalatroCardEdition.Polychrome => BalatroCardKeywords.Polychrome,
            BalatroCardEdition.Negative => BalatroCardKeywords.Negative,
            _ => CardKeyword.None
        };
    }

    public static int GetPlayCountBonus(CardModel? card)
    {
        if (card is YuWanCardModel yuWanCard)
        {
            return yuWanCard.GetBalatroPlayCountBonus();
        }

        return GetEdition(card) == BalatroCardEdition.Polychrome ? 1 : 0;
    }

    public static IEnumerable<CardModel> GetSelectableHandCards(Player owner, CardModel source)
    {
        return PileType.Hand.GetPile(owner).Cards.Where(card => card != source);
    }

    private static bool TryApplyEditionToSingleCard(CardModel card, BalatroCardEdition edition)
    {
        if (!CanApplyEdition(card, edition))
        {
            return false;
        }

        if (card is YuWanCardModel yuWanCard)
        {
            return yuWanCard.TryApplyBalatroEdition(edition);
        }

        GenericEdition[card] = (int)edition;
        CardKeyword keyword = GetEditionKeyword(edition);
        if (keyword != CardKeyword.None && !card.Keywords.Contains(keyword))
        {
            card.AddKeyword(keyword);
        }

        if (edition == BalatroCardEdition.Foil && !GenericFoilApplied[card])
        {
            ApplyFoilEdition(card);
            GenericFoilApplied[card] = true;
        }

        return true;
    }

    internal static void ApplyFoilEdition(CardModel card)
    {
        foreach (DynamicVar dynamicVar in card.DynamicVars.Values)
        {
            if (dynamicVar is EnergyVar || dynamicVar.BaseValue <= 0)
            {
                continue;
            }

            decimal increase = Math.Max(1m, Math.Floor(dynamicVar.BaseValue * 0.2m));
            dynamicVar.BaseValue += increase;
        }
    }

    private static void Upsert<T>(List<SavedProperties.SavedProperty<T>> list, string name, T value)
    {
        int existingIndex = list.FindIndex(prop => prop.name == name);
        SavedProperties.SavedProperty<T> property = new(name, value);
        if (existingIndex >= 0)
        {
            list[existingIndex] = property;
            return;
        }

        list.Add(property);
    }
}
