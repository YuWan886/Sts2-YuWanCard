using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using YuWanCard.Events;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllSharedEvents), MethodType.Getter)]
public class BlacksmithEventPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<EventModel> __result)
    {
        var list = __result.ToList();
        if (!list.Any(e => e is Blacksmith))
        {
            if (ModelDb.Contains(typeof(Blacksmith)))
            {
                list.Add(ModelDb.Event<Blacksmith>());
                __result = list;
            }
        }
    }
}

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public class BlacksmithRegistrationPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        if (!ModelDb.Contains(typeof(Blacksmith)))
        {
            ModelDb.Inject(typeof(Blacksmith));
            MainFile.Logger.Info("Blacksmith event registered to ModelDb");
        }
    }
}
