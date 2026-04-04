using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Random;
using YuWanCard.Characters;

namespace YuWanCard.Patches;

public static class PigPotionHelper
{
    public static IEnumerable<CardModel> GetCardsForPotion(Player player, CardType cardType)
    {
        if (player.Character is Pig)
        {
            return PigAllCards.GetAllUnlockedCardsByType(player, cardType);
        }
        
        return player.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(c => c.Type == cardType);
    }
}

[HarmonyPatch(typeof(AttackPotion), "OnUse")]
public static class AttackPotionTranspiler
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var helperMethod = AccessTools.Method(typeof(PigPotionHelper), nameof(PigPotionHelper.GetCardsForPotion));
        var getUnlockedCardsMethod = AccessTools.Method(typeof(CardPoolModel), "GetUnlockedCards");
        
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo m && m == getUnlockedCardsMethod)
            {
                yield return new CodeInstruction(OpCodes.Call, helperMethod);
                yield return new CodeInstruction(OpCodes.Ldc_I4, (int)CardType.Attack);
                continue;
            }
            yield return instruction;
        }
    }
}

[HarmonyPatch(typeof(SkillPotion), "OnUse")]
public static class SkillPotionTranspiler
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var helperMethod = AccessTools.Method(typeof(PigPotionHelper), nameof(PigPotionHelper.GetCardsForPotion));
        var getUnlockedCardsMethod = AccessTools.Method(typeof(CardPoolModel), "GetUnlockedCards");
        
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo m && m == getUnlockedCardsMethod)
            {
                yield return new CodeInstruction(OpCodes.Call, helperMethod);
                yield return new CodeInstruction(OpCodes.Ldc_I4, (int)CardType.Skill);
                continue;
            }
            yield return instruction;
        }
    }
}

[HarmonyPatch(typeof(PowerPotion), "OnUse")]
public static class PowerPotionTranspiler
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var helperMethod = AccessTools.Method(typeof(PigPotionHelper), nameof(PigPotionHelper.GetCardsForPotion));
        var getUnlockedCardsMethod = AccessTools.Method(typeof(CardPoolModel), "GetUnlockedCards");
        
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo m && m == getUnlockedCardsMethod)
            {
                yield return new CodeInstruction(OpCodes.Call, helperMethod);
                yield return new CodeInstruction(OpCodes.Ldc_I4, (int)CardType.Power);
                continue;
            }
            yield return instruction;
        }
    }
}

[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.GetDistinctForCombat))]
public static class CardFactoryGetDistinctForCombatPatch
{
    [HarmonyPrefix]
    public static bool Prefix(Player player, IEnumerable<CardModel> cards, int count, Rng rng, ref IEnumerable<CardModel> __result)
    {
        var cardList = cards.ToList();
        
        if (cardList.Count == 0)
        {
            __result = [];
            return false;
        }

        var filtered = FilterForCombatWithBasicFallback(cardList).ToList();
        
        if (filtered.Count == 0)
        {
            __result = [];
            return false;
        }

        filtered = CardFactory.FilterForPlayerCount(player.RunState, filtered).ToList();
        var selected = filtered.TakeRandom(count, rng);
        __result = selected.Select(c => player.Creature!.CombatState!.CreateCard(c, player));
        
        return false;
    }

    private static IEnumerable<CardModel> FilterForCombatWithBasicFallback(IEnumerable<CardModel> cards)
    {
        var cardList = cards.ToList();
        var filtered = cardList.Where(c => c.CanBeGeneratedInCombat && c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Ancient && c.Rarity != CardRarity.Event).Distinct().ToList();
        
        if (filtered.Count > 0)
        {
            return filtered;
        }

        var basicCards = cardList.Where(c => c.CanBeGeneratedInCombat && c.Rarity == CardRarity.Basic).Distinct().ToList();
        if (basicCards.Count > 0)
        {
            return basicCards;
        }

        return cardList.Where(c => c.CanBeGeneratedInCombat).Distinct();
    }
}
