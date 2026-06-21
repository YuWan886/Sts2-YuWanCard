using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;
using YuWanCard.Core.Abstracts;
using YuWanCard.Utils;

namespace YuWanCard.Core.Patches;

/// <summary>
/// Vanilla ancient compendium logic intersects an ancient's relic options with
/// ModelDb.AllRelics. If a mod relic misses that cache path, the ancient shows up
/// but its relic list is empty. For custom ancients, rebuild the subcategory from
/// AllPossibleOptions directly so their ancient relics always render.
/// </summary>
[HarmonyPatch(typeof(NRelicCollectionCategory), nameof(NRelicCollectionCategory.LoadRelics))]
static class AncientRelicCompendiumPatch
{
    private static readonly FieldInfo? HeaderLabelField =
        YuWanReflectionHelper.GetPrivateField(typeof(NRelicCollectionCategory), "_headerLabel");

    private static readonly FieldInfo? SpacerField =
        YuWanReflectionHelper.GetPrivateField(typeof(NRelicCollectionCategory), "_spacer");

    private static readonly FieldInfo? SubCategoriesField =
        YuWanReflectionHelper.GetPrivateField(typeof(NRelicCollectionCategory), "_subCategories");

    private static readonly MethodInfo? LoadSubcategoryMethod =
        YuWanReflectionHelper.GetPrivateMethod(
            typeof(NRelicCollectionCategory),
            "LoadSubcategory",
            [typeof(NRelicCollection), typeof(LocString), typeof(IEnumerable<RelicModel>), typeof(HashSet<RelicModel>), typeof(HashSet<RelicModel>)]);

    [HarmonyPostfix]
    static void AddCustomAncientRelicSubcategories(
        NRelicCollectionCategory __instance,
        RelicRarity relicRarity,
        NRelicCollection collection,
        HashSet<RelicModel> seenRelics,
        UnlockState unlockState,
        HashSet<RelicModel> allUnlockedRelics)
    {
        if (relicRarity != RelicRarity.Ancient)
        {
            return;
        }

        try
        {
            foreach (YuWanAncientModel customAncient in ModelDb.AllSharedAncients.OfType<YuWanAncientModel>())
            {
                if (!unlockState.SharedAncients.Contains(customAncient))
                {
                    continue;
                }

                if (HasExistingSubcategoryForAncient(__instance, customAncient))
                {
                    continue;
                }

                RelicModel[] relics = customAncient.AllPossibleOptions
                    .Select(static option => option.Relic?.CanonicalInstance)
                    .OfType<RelicModel>()
                    .Where(static relic => relic.Rarity == RelicRarity.Ancient)
                    .DistinctBy(static relic => relic.Id)
                    .OrderBy(static relic => relic.Title.GetFormattedText(), LocManager.Instance.StringComparer)
                    .ToArray();

                if (relics.Length == 0)
                {
                    continue;
                }

                NRelicCollectionCategory? subcategory = CreateSubcategoryNode();
                if (subcategory == null)
                {
                    return;
                }

                RegisterSubcategory(__instance, subcategory);

                bool revealed = SaveManager.Instance.Progress.AncientStats.ContainsKey(customAncient.Id)
                    || relics.Any(seenRelics.Contains);
                LocString unknownAncient = new("relic_collection", "UNKNOWN_ANCIENT");
                LocString header = new("relic_collection", "ANCIENT_SUBCATEGORY");
                header.Add("Ancient", revealed ? customAncient.Title : unknownAncient);

                LoadSubcategoryMethod?.Invoke(subcategory, [collection, header, relics, seenRelics, allUnlockedRelics]);
                subcategory.LoadIcon(customAncient.RunHistoryIcon);
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[AncientRelicCompendiumPatch] Failed to add custom ancient relic categories: {ex}");
        }
    }

    private static bool HasExistingSubcategoryForAncient(NRelicCollectionCategory category, AncientEventModel ancient)
    {
        if (SubCategoriesField?.GetValue(category) is not List<NRelicCollectionCategory> subcategories)
        {
            return false;
        }

        string title = ancient.Title.GetFormattedText();
        foreach (NRelicCollectionCategory subcategory in subcategories)
        {
            if (!GodotObject.IsInstanceValid(subcategory))
            {
                continue;
            }

            if (HeaderLabelField?.GetValue(subcategory) is not MegaCrit.Sts2.addons.mega_text.MegaRichTextLabel headerLabel)
            {
                continue;
            }

            if (headerLabel.Text.Contains(title, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static NRelicCollectionCategory? CreateSubcategoryNode()
    {
        PackedScene? scene = PreloadManager.Cache.GetScene(NRelicCollectionCategory.scenePath);
        return scene?.Instantiate<NRelicCollectionCategory>(PackedScene.GenEditState.Disabled);
    }

    private static void RegisterSubcategory(NRelicCollectionCategory parent, NRelicCollectionCategory subcategory)
    {
        parent.AddChildSafely(subcategory);

        if (SubCategoriesField?.GetValue(parent) is List<NRelicCollectionCategory> subcategories)
        {
            subcategories.Add(subcategory);
        }

        int insertIndex = HeaderLabelField?.GetValue(parent) is Control headerLabel
            ? Math.Min(parent.GetChildCount() - 1, headerLabel.GetIndex() + parent.GetChildren().OfType<NRelicCollectionCategory>().Count())
            : parent.GetChildCount() - 1;
        parent.MoveChildSafely(subcategory, insertIndex);

        if (SpacerField?.GetValue(subcategory) is Control spacer)
        {
            spacer.Visible = true;
        }
    }
}
