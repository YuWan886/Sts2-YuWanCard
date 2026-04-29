using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Cards;

namespace YuWanCard.Utils;

public static class CardUtils
{
    private static readonly string[] DamageVarNames = ["Damage", "CalculatedDamage", "OstyDamage", "ExtraDamage"];

    public static readonly List<Func<CardModel>> FoodPigCardFactories =
    [
        ModelDb.Card<PigChop>,
        ModelDb.Card<PigPudding>,
        ModelDb.Card<TiramisuPig>,
        ModelDb.Card<PigSouffle>,
        ModelDb.Card<PigBlueberryCake>
    ];

    public static CardModel GetRandomFoodPigCardCanonical(Player player)
    {
        var factory = FoodPigCardFactories
            .OrderBy(_ => player.RunState.Rng.CombatCardGeneration.NextFloat())
            .First();
        return factory();
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
