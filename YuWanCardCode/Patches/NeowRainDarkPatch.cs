using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using YuWanCard.Cards;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(Neow))]
class NeowRainDarkPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("PositiveOptions", MethodType.Getter)]
    static void AddRainDarkToPositiveOptions(Neow __instance, ref IEnumerable<EventOption> __result)
    {
        __result = __result.Concat(new[] { CreateRainDarkOption(__instance) });
    }

    private static EventOption CreateRainDarkOption(Neow neow)
    {
        LocString selectTitle = new LocString("cards", "YUWANCARD-RAIN_DARK_SELECT.title");
        LocString selectDescription = new LocString("cards", "YUWANCARD-RAIN_DARK_SELECT.description");

        return new EventOption(
            neow,
            async () =>
            {
                if (neow.Owner != null)
                {
                    var card = neow.Owner.RunState.CreateCard(ModelDb.Card<RainDark>(), neow.Owner);
                    CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck));
                    
                    YuWanReflectionHelper.CallPrivateMethod(neow, "SetEventFinished", new LocString("events", "NEOW.pages.DONE.POSITIVE.description"));
                }
            },
            selectTitle,
            selectDescription,
            "YUWANCARD-RAIN_DARK",
            HoverTipFactory.FromCardWithCardHoverTips<RainDark>()
        );
    }
}
