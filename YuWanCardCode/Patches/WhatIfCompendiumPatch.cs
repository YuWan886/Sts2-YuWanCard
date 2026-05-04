using System.Reflection;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using YuWanCard.RelicPools;
using YuWanCard.Utils;

namespace YuWanCard.Patches;

[HarmonyPatch(typeof(NRelicCollectionCategory), "LoadRelics")]
class WhatIfCompendiumPatch
{
    private static readonly MethodInfo CreateForSubcategoryMethod =
        typeof(NRelicCollectionCategory).GetMethod("CreateForSubcategory", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo LoadSubcategoryMethod =
        typeof(NRelicCollectionCategory).GetMethod("LoadSubcategory", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [HarmonyPostfix]
    static void AddWhatIfSubcategory(
        NRelicCollectionCategory __instance,
        RelicRarity relicRarity,
        NRelicCollection collection,
        HashSet<RelicModel> seenRelics,
        HashSet<RelicModel> allUnlockedRelics)
    {
        if (relicRarity != RelicRarity.Event)
            return;

        var pool = ModelDb.RelicPool<WhatIfRelicPool>();
        var whatIfIds = pool.AllRelicIds;
        var whatIfRelics = pool.AllRelics
            .Select(r => r.CanonicalInstance)
            .Distinct()
            .Where(r => allUnlockedRelics.Contains(r))
            .ToList();

        if (whatIfRelics.Count == 0)
            return;

        // Remove What If relics from the Event tab's general grid
        var relicsContainer = YuWanReflectionHelper.GetPrivateField<GridContainer>(__instance, "_relicsContainer");
        if (relicsContainer != null)
        {
            foreach (var child in relicsContainer.GetChildren().OfType<NRelicCollectionEntry>().ToList())
            {
                if (child.relic != null && whatIfIds.Contains(child.relic.Id))
                {
                    child.QueueFreeSafely();
                }
            }
        }

        // Create a subcategory for What If relics
        whatIfRelics.Sort((a, b) =>
            LocManager.Instance.StringComparer.Compare(
                a.Title.GetFormattedText(),
                b.Title.GetFormattedText()));

        var subCategory = (NRelicCollectionCategory)CreateForSubcategoryMethod.Invoke(__instance, null)!;
        __instance.AddChildSafely(subCategory);

        var header = new LocString("relics", "YUWANCARD-WHAT_IF_CATEGORY.header");
        LoadSubcategoryMethod.Invoke(subCategory, [collection, header, whatIfRelics, seenRelics, allUnlockedRelics]);
    }
}
