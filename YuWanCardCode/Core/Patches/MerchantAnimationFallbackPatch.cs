using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(NMerchantCharacter), "PlayAnimation")]
public static class MerchantAnimationFallbackPatch
{
    private const string FallbackAnimation = "idle_loop";

    [HarmonyPrefix]
    public static bool Prefix(NMerchantCharacter __instance, string anim, bool loop)
    {
        var child = __instance.GetChild(0);
        if (child == null) return true;
        if (child.GetClass() != "SpineSprite") return false;

        var megaSprite = new MegaSprite(child);
        if (!megaSprite.HasAnimation(anim))
        {
            if (megaSprite.HasAnimation(FallbackAnimation))
            {
                megaSprite.GetAnimationState().SetAnimation(FallbackAnimation, loop);
                return false;
            }
        }

        return true;
    }
}
