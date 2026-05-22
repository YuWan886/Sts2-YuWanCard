using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.addons.mega_text;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Saves;
using YuWanCard.Core.Abstracts;
using YuWanCard.Utils;

namespace YuWanCard.Core.Patches;

[HarmonyPatch]
static class CustomRelicRarityCompendiumPatch
{
    private sealed record CustomRarityGroup(YuWanCustomRelicRarity Rarity, List<RelicModel> Relics);

    private static readonly ConditionalWeakTable<NRelicCollection, List<NRelicCollectionCategory>> CustomCategories = new();

    private static readonly ConditionalWeakTable<NRelicCollectionCategory, object> ManagedCustomCategoryMarkers = new();

    private static readonly MethodInfo? LoadSubcategoryMethod =
        YuWanReflectionHelper.GetPrivateMethod(typeof(NRelicCollectionCategory), "LoadSubcategory");

    private static readonly MethodInfo? SetRarityVisualsMethod =
        YuWanReflectionHelper.GetPrivateMethod(typeof(NInspectRelicScreen), "SetRarityVisuals");

    private static readonly FieldInfo? RelicsField =
        YuWanReflectionHelper.GetPrivateField(typeof(NRelicCollection), "_relics");

    private static bool TryGetCustomRarity(RelicModel relic, out YuWanCustomRelicRarity? rarity)
    {
        rarity = (relic.CanonicalInstance as YuWanRelicModel)?.CustomRarity;
        return rarity != null;
    }

    private static List<CustomRarityGroup>? _cachedCustomRarityGroups;

    private static List<CustomRarityGroup> GetCustomRarityGroups()
    {
        return _cachedCustomRarityGroups ??= ModelDb.AllRelics
            .OfType<YuWanRelicModel>()
            .Select(relic => new
            {
                Relic = relic.CanonicalInstance,
                Rarity = relic.CustomRarity
            })
            .Where(entry => entry.Rarity != null)
            .GroupBy(entry => entry.Rarity!.Id, StringComparer.Ordinal)
            .Select(group =>
            {
                var rarity = group.First().Rarity!;
                var relics = group
                    .Select(entry => entry.Relic)
                    .DistinctBy(relic => relic.Id)
                    .OrderBy(relic => relic.Title.GetFormattedText(), LocManager.Instance.StringComparer)
                    .ToList();
                return new CustomRarityGroup(rarity, relics);
            })
            .OrderBy(group => group.Rarity.SortOrder)
            .ThenBy(group => group.Rarity.CreateHeader().GetFormattedText(), LocManager.Instance.StringComparer)
            .ToList();
    }

    private static bool IsManagedCustomCategory(NRelicCollectionCategory category)
    {
        return ManagedCustomCategoryMarkers.TryGetValue(category, out _);
    }

    private static List<NRelicCollectionCategory> EnsureCustomCategoryNodes(
        NRelicCollection collection,
        NRelicCollectionCategory eventCategory,
        int requiredCount)
    {
        var categories = CustomCategories.GetValue(collection, _ => []);
        categories.RemoveAll(category => !GodotObject.IsInstanceValid(category));

        var scene = PreloadManager.Cache.GetScene(NRelicCollectionCategory.scenePath);
        if (scene == null)
            return categories;

        Control anchor = categories.LastOrDefault() ?? eventCategory;
        while (categories.Count < requiredCount)
        {
            var category = scene.Instantiate<NRelicCollectionCategory>(PackedScene.GenEditState.Disabled);
            category.Name = $"CustomRelicRarity{categories.Count}";
            category.Visible = false;
            anchor.AddSibling(category, forceReadableName: true);
            anchor = category;
            categories.Add(category);
            ManagedCustomCategoryMarkers.Add(category, new object());
        }

        return categories;
    }

    private static void RebuildFocusNavigation(IEnumerable<NRelicCollectionCategory> categories)
    {
        var rows = new List<IReadOnlyList<Control>>();
        foreach (var category in categories)
        {
            if (!GodotObject.IsInstanceValid(category) || !category.Visible)
                continue;

            rows.AddRange(category.GetGridItems().Where(row => row.Count > 0));
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (var columnIndex = 0; columnIndex < row.Count; columnIndex++)
            {
                var control = row[columnIndex];
                control.FocusNeighborLeft = (columnIndex > 0 ? row[columnIndex - 1] : row[^1]).GetPath();
                control.FocusNeighborRight = (columnIndex < row.Count - 1 ? row[columnIndex + 1] : row[0]).GetPath();
                control.FocusNeighborTop = rowIndex > 0
                    ? (columnIndex < rows[rowIndex - 1].Count ? rows[rowIndex - 1][columnIndex] : rows[rowIndex - 1][^1]).GetPath()
                    : control.GetPath();
                control.FocusNeighborBottom = rowIndex < rows.Count - 1
                    ? (columnIndex < rows[rowIndex + 1].Count ? rows[rowIndex + 1][columnIndex] : rows[rowIndex + 1][^1]).GetPath()
                    : control.GetPath();
            }
        }
    }

    [HarmonyPatch(typeof(NRelicCollection), "LoadRelics")]
    [HarmonyPostfix]
    static void LoadCustomRarityCategoriesAsync(
        NRelicCollection __instance,
        NRelicCollectionCategory ____starter,
        NRelicCollectionCategory ____common,
        NRelicCollectionCategory ____uncommon,
        NRelicCollectionCategory ____rare,
        NRelicCollectionCategory ____shop,
        NRelicCollectionCategory ____ancient,
        NRelicCollectionCategory ____event,
        ref Task __result)
    {
        var customRarityGroups = GetCustomRarityGroups();
        var customCategories = EnsureCustomCategoryNodes(__instance, ____event, customRarityGroups.Count);

        __result = ContinueLoadingCustomRarityCategories(
            __result,
            __instance,
            customRarityGroups,
            customCategories,
            [____starter, ____common, ____uncommon, ____rare, ____shop, ____ancient, ____event]);
    }

    private static async Task ContinueLoadingCustomRarityCategories(
        Task originalTask,
        NRelicCollection collection,
        IReadOnlyList<CustomRarityGroup> customRarityGroups,
        IReadOnlyList<NRelicCollectionCategory> customCategories,
        IReadOnlyList<NRelicCollectionCategory> vanillaCategories)
    {
        foreach (var category in customCategories)
        {
            category.Visible = false;
            category.Modulate = Colors.Transparent;
            category.ClearRelics();
        }

        await originalTask;

        try
        {
            if (RelicsField?.GetValue(collection) is List<RelicModel> relics)
            {
                relics.RemoveAll(relic => TryGetCustomRarity(relic, out _));
            }

            var discoveredRelics = SaveManager.Instance.Progress.DiscoveredRelics
                .Select(ModelDb.GetByIdOrNull<RelicModel>)
                .OfType<RelicModel>()
                .ToHashSet();
            var unlockedRelics = SaveManager.Instance.GenerateUnlockStateFromProgress().Relics.ToHashSet();

            for (var i = 0; i < customRarityGroups.Count; i++)
            {
                var group = customRarityGroups[i];
                var category = customCategories[i];

                LoadSubcategoryMethod?.Invoke(category,
                    [collection, group.Rarity.CreateHeader(), group.Relics, discoveredRelics, unlockedRelics]);

                category.Visible = true;
                category.Modulate = Colors.White;
            }

            RebuildFocusNavigation(vanillaCategories.Concat(customCategories.Where(category => category.Visible)));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[CustomRelicRarityCompendiumPatch] Failed to build custom relic rarity categories: {ex}");
        }
    }

    [HarmonyPatch(typeof(NRelicCollection), "ClearRelics")]
    [HarmonyPostfix]
    static void ClearCustomRarityCategories(NRelicCollection __instance)
    {
        if (!CustomCategories.TryGetValue(__instance, out var categories))
            return;

        foreach (var category in categories)
        {
            if (!GodotObject.IsInstanceValid(category))
                continue;

            category.ClearRelics();
            category.Visible = false;
        }
    }

    [HarmonyPatch(typeof(NRelicCollectionCategory), "LoadRelicNodes")]
    [HarmonyPrefix]
    static void FilterCustomRarityRelics(
        NRelicCollectionCategory __instance,
        ref IEnumerable<RelicModel> relics)
    {
        if (IsManagedCustomCategory(__instance))
            return;

        relics = relics.Where(relic => !TryGetCustomRarity(relic, out _));
    }

    [HarmonyPatch(typeof(NInspectRelicScreen), "UpdateRelicDisplay")]
    [HarmonyPostfix]
    static void ApplyCustomRarityInspectLabel(
        NInspectRelicScreen __instance,
        IReadOnlyList<RelicModel> ____relics,
        int ____index,
        HashSet<RelicModel> ____allUnlockedRelics,
        MegaLabel ____rarityLabel)
    {
        if (____index < 0 || ____index >= ____relics.Count)
            return;

        var relic = ____relics[____index];
        if (!____allUnlockedRelics.Contains(relic.CanonicalInstance) || !SaveManager.Instance.IsRelicSeen(relic))
            return;

        if (!TryGetCustomRarity(relic, out var rarity) || rarity == null)
            return;

        ____rarityLabel.SetTextAutoSize(rarity.CreateDisplayLabel().GetFormattedText());
        SetRarityVisualsMethod?.Invoke(__instance, [rarity.VisualRarity]);
    }

    [HarmonyPatch(typeof(RelicModel), "get_IsTradable")]
    [HarmonyPostfix]
    static void DisableTradingForCustomRarityRelics(RelicModel __instance, ref bool __result)
    {
        if (TryGetCustomRarity(__instance, out _))
        {
            __result = false;
        }
    }
}
