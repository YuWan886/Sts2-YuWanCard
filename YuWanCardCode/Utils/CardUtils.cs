using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Utils;

public static class CardUtils
{
    private static readonly string[] DamageVarNames = ["Damage", "CalculatedDamage", "OstyDamage", "ExtraDamage"];

    private static List<CardModel>? _foodPigCards;
    private static List<CardModel> FoodPigCards
    {
        get
        {
            if (_foodPigCards == null)
            {
                _foodPigCards = ModelDb.AllCards
                    .Where(c => c.Tags.Contains(YuWanTags.FoodPig))
                    .ToList();
            }
            return _foodPigCards;
        }
    }

    public static CardModel GetRandomFoodPigCardCanonical(Player player)
    {
        return FoodPigCards
            .OrderBy(_ => player.RunState.Rng.CombatCardGeneration.NextFloat())
            .First();
    }

    public static CardModel CreateRandomFoodPigCard(Player player)
    {
        return player.RunState.CreateCard(GetRandomFoodPigCardCanonical(player), player);
    }

    public static bool HasDamageVariable(CardModel? card)
    {
        if (card == null)
        {
            return false;
        }

        var vars = card.DynamicVars;
        foreach (var varName in DamageVarNames)
        {
            if (vars.ContainsKey(varName))
            {
                return true;
            }
        }

        return false;
    }
}
