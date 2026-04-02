using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public class EnchantmentRegistrationPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        var assembly = typeof(EnchantmentRegistrationPatch).Assembly;
        var enchantmentBaseType = typeof(EnchantmentModel);

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsClass && !type.IsAbstract && enchantmentBaseType.IsAssignableFrom(type))
            {
                if (!ModelDb.Contains(type))
                {
                    ModelDb.Inject(type);
                }
            }
        }
    }
}
