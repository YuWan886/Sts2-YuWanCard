using HarmonyLib;
using MegaCrit.Sts2.Core.Events;

namespace YuWanCard.Core.Patches;

[HarmonyPatch(typeof(EventOption), "AddLocVars")]
static class EventOptionNullDescriptionFix
{
    static bool Prefix(EventOption __instance)
    {
        return __instance.Description != null;
    }
}

[HarmonyPatch(typeof(EventOption), "ToString")]
static class EventOptionToStringFix
{
    static string Postfix(string __result, EventOption __instance)
    {
        var title = __instance.Title?.GetRawText() ?? "null";
        var description = __instance.Description?.GetRawText() ?? "null";
        var textKey = __instance.TextKey ?? "null";
        return $"EventOption title: {title} description: {description} textKey: {textKey}";
    }
}
