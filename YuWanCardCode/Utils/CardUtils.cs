using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Utils;

public static class CardUtils
{
    private static readonly string[] DamageVarNames = ["Damage", "CalculatedDamage", "OstyDamage", "ExtraDamage"];

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
