using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Core.Abstracts;
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
        Upsert(save.Props.bools, GenericFoilAppliedSavedPropertyName, IsFoilApplied(card));
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

        SetEdition(card, edition);

        // The saved bool only describes the old instance's runtime marker. A deserialized card
        // rebuilds its stats from the canonical model, so foil must be considered unapplied here.
        SetFoilApplied(card, false);
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

    public static bool HasEdition(CardModel? card)
    {
        return GetEdition(card) != BalatroCardEdition.None;
    }

    public static void CopyEditionStateToClone(CardModel? source, CardModel? clone)
    {
        if (source == null || clone == null || ReferenceEquals(source, clone))
        {
            return;
        }

        BalatroCardEdition edition = GetEdition(source);
        if (edition == BalatroCardEdition.None)
        {
            return;
        }

        SetEdition(clone, edition);
        EnsureEditionKeyword(clone, edition);
        SetFoilApplied(clone, IsFoilApplied(source));
    }

    public static void RefreshEditionAfterCardStateRebuild(CardModel? card)
    {
        if (card == null)
        {
            return;
        }

        BalatroCardEdition edition = GetEdition(card);
        if (edition == BalatroCardEdition.None)
        {
            SetFoilApplied(card, false);
            return;
        }

        EnsureEditionKeyword(card, edition);

        if (edition != BalatroCardEdition.Foil)
        {
            SetFoilApplied(card, false);
            return;
        }

        SetFoilApplied(card, false);
        ApplyFoilEdition(card);
        SetFoilApplied(card, true);
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

        SetEdition(card, edition);
        EnsureEditionKeyword(card, edition);

        if (edition == BalatroCardEdition.Foil && !IsFoilApplied(card))
        {
            ApplyFoilEdition(card);
            SetFoilApplied(card, true);
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

    private static bool IsFoilApplied(CardModel card)
    {
        return card is YuWanCardModel yuWanCard
            ? yuWanCard.YUWANCARD_FoilApplied
            : GenericFoilApplied[card];
    }

    private static void SetEdition(CardModel card, BalatroCardEdition edition)
    {
        if (card is YuWanCardModel yuWanCard)
        {
            yuWanCard.YUWANCARD_Edition = (int)edition;
            return;
        }

        GenericEdition[card] = (int)edition;
    }

    private static void SetFoilApplied(CardModel card, bool value)
    {
        if (card is YuWanCardModel yuWanCard)
        {
            yuWanCard.YUWANCARD_FoilApplied = value;
            return;
        }

        GenericFoilApplied[card] = value;
    }

    private static void EnsureEditionKeyword(CardModel card, BalatroCardEdition edition)
    {
        CardKeyword keyword = GetEditionKeyword(edition);
        if (keyword != CardKeyword.None && !card.Keywords.Contains(keyword))
        {
            card.AddKeyword(keyword);
        }
    }
}
