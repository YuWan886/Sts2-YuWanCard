using System.Reflection;
using System.Reflection.Emit;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using YuWanCard.Config;

namespace YuWanCard;

/// <summary>
/// Registers YuWanCard settings with STS2-RitsuLib via runtime reflection-emitted
/// provider types. Supports boolean toggles and double sliders.
/// </summary>
internal static class ConfigRegistrar
{
    private const string ModId = MainFile.ModId;
    private const string RootPageId = "yuwan_card";
    private const string ContentPageId = "game_content";
    private const string ColorlessCardsPageId = "content_colorless_cards";

    private static bool s_ritsuRegistered;
    private static Type[]? s_dynamicRitsuProviderTypes;

    private sealed record ConfigPageDefinition(
        string TypeName,
        string PageId,
        string Title,
        string? Description,
        int SortOrder,
        string? ParentPageId = null,
        string? ModDisplayName = null,
        string? TitleLocKey = null,
        string? DescriptionLocKey = null);

    private sealed record ConfigSectionDefinition(
        string PageId,
        string SectionId,
        string Title,
        string? Description,
        int SortOrder,
        string? TitleLocKey = null,
        string? DescriptionLocKey = null);

    private sealed record ToggleSettingDefinition(
        string PropertyName,
        string RitsuPageId,
        string RitsuSectionId,
        string RitsuId,
        string DataKey,
        string Label,
        string? Description,
        int Order,
        string? LabelLocKey = null,
        string? DescriptionLocKey = null);

    private sealed record SliderSettingDefinition(
        string PropertyName,
        string RitsuPageId,
        string RitsuSectionId,
        string RitsuId,
        string DataKey,
        string Label,
        string? Description,
        double Min,
        double Max,
        double Step,
        string Format,
        int Order,
        string? LabelLocKey = null,
        string? DescriptionLocKey = null);

    private sealed record SubpageSettingDefinition(
        string RitsuPageId,
        string RitsuSectionId,
        string EntryId,
        string TargetPageId,
        string MethodName,
        string Label,
        string? Description,
        string? ButtonText,
        int Order,
        string? LabelLocKey = null,
        string? DescriptionLocKey = null,
        string? ButtonTextLocKey = null);

    private sealed record CustomEntrySettingDefinition(
        string RitsuPageId,
        string RitsuSectionId,
        string EntryId,
        string MethodName,
        string Label,
        string? Description,
        int Order,
        string? LabelLocKey = null,
        string? DescriptionLocKey = null);

    private static readonly ConfigPageDefinition[] RitsuPages =
    [
        new("YuWanCardRitsuConfigProvider", RootPageId, "YuWanCard 设置", null, 0, null, "YuWanCard", "YUWANCARD-RITSU_ROOT_PAGE.title"),
        new("YuWanCardRitsuContentConfigProvider", ContentPageId, "游戏内容设置", "控制本模组敌人、事件和新增无色卡牌是否会出现在对局中", 100, RootPageId, "YuWanCard",
            "YUWANCARD-RITSU_GAME_CONTENT_PAGE.title", "YUWANCARD-RITSU_GAME_CONTENT_PAGE.desc"),
        new("YuWanCardRitsuColorlessCardConfigProvider", ColorlessCardsPageId, "无色卡牌设置", "控制本模组新增无色卡牌是否会出现在对局中。悬停按钮可查看卡牌提示。", 200, ContentPageId, "YuWanCard",
            "YUWANCARD-RITSU_COLORLESS_PAGE.title", "YUWANCARD-RITSU_COLORLESS_PAGE.desc"),
    ];

    private static readonly ConfigSectionDefinition[] RitsuSections =
    [
        new(RootPageId, "display", "显示设置", null, 0, "YUWANCARD-RITSU_DISPLAY_SECTION.title"),
        new(RootPageId, "updates", "更新设置", null, 100, "YUWANCARD-RITSU_UPDATES_SECTION.title"),
        new(RootPageId, "gameplay", "游戏设置", null, 200, "YUWANCARD-RITSU_GAMEPLAY_SECTION.title"),
        new(ContentPageId, "cards", "卡牌", null, 0, "YUWANCARD-RITSU_GAME_CONTENT_CARDS.title"),
        new(ContentPageId, "enemy_encounters", "敌人遭遇", null, 100, "YUWANCARD-RITSU_GAME_CONTENT_ENCOUNTERS.title"),
        new(ContentPageId, "events", "事件", null, 200, "YUWANCARD-RITSU_GAME_CONTENT_EVENTS.title"),
        new(ContentPageId, "ancients", "先古", null, 300, "YUWANCARD-RITSU_GAME_CONTENT_ANCIENTS.title"),
        new(ColorlessCardsPageId, YuWanColorlessCardCatalog.SectionId, "无色卡牌画廊", "按按钮开启或关闭对应卡牌。", 0,
            "YUWANCARD-RITSU_COLORLESS_SECTION.title", "YUWANCARD-RITSU_COLORLESS_SECTION.desc"),
    ];

    // Boolean toggle settings registered into RitsuLib pages and sections.
    private static readonly ToggleSettingDefinition[] ToggleProps =
    [
        new("EnableDeathEffect", RootPageId, "display", "enable_death_effect", "config_enable_death_effect", "死亡特效", "击败敌人时显示死亡特效", 0,
            "YUWANCARD-ENABLE_DEATH_EFFECT.title", "YUWANCARD-ENABLE_DEATH_EFFECT.hover.desc"),
        new("EnableCustomCursor", RootPageId, "display", "enable_custom_cursor", "config_enable_custom_cursor", "自定义鼠标指针", "用猪猪主题指针替换游戏默认鼠标指针", 1,
            "YUWANCARD-ENABLE_CUSTOM_CURSOR.title", "YUWANCARD-ENABLE_CUSTOM_CURSOR.hover.desc"),
        new("EnablePigScaleWithHp", RootPageId, "display", "enable_pig_scale_with_hp", "config_enable_pig_scale_with_hp", "猪体型随血量变化", "关闭后猪角色在战斗中保持固定体型", 3,
            "YUWANCARD-ENABLE_PIG_SCALE_WITH_HP.title", "YUWANCARD-ENABLE_PIG_SCALE_WITH_HP.hover.desc"),
        new("EnableAutoUpdateCheck", RootPageId, "updates", "enable_auto_update", "config_enable_auto_update_check", "自动检查更新", "启动时自动检查模组更新", 0,
            "YUWANCARD-ENABLE_AUTO_UPDATE_CHECK.title", "YUWANCARD-ENABLE_AUTO_UPDATE_CHECK.hover.desc"),
        new("EnableSevenCursesRing", RootPageId, "gameplay", "enable_seven_curses_ring", "config_enable_seven_curses_ring", "七咒之戒", "在Neow处可选择七咒之戒", 0,
            "YUWANCARD-ENABLE_SEVEN_CURSES_RING.title", "YUWANCARD-ENABLE_SEVEN_CURSES_RING.hover.desc"),
        new("EnableMaliceSelection", RootPageId, "gameplay", "enable_malice_selection", "config_enable_malice_selection", "恶意难度选择", "在角色选择界面显示恶意难度选择面板", 1,
            "YUWANCARD-ENABLE_MALICE_SELECTION.title", "YUWANCARD-ENABLE_MALICE_SELECTION.hover.desc"),
        new("EnablePigRewardAllCardPools", RootPageId, "gameplay", "enable_pig_reward_all_card_pools", "config_enable_pig_reward_all_card_pools", "猪奖励出现全部卡池", "启用后，猪角色的遭遇卡牌奖励会固定保留 1 张猪卡，其余位置改为随机其他卡池的卡", 2,
            "YUWANCARD-ENABLE_PIG_REWARD_ALL_CARD_POOLS.title", "YUWANCARD-ENABLE_PIG_REWARD_ALL_CARD_POOLS.hover.desc"),
        new("EnableYuWanEnemyEncounters", ContentPageId, "enemy_encounters", "enable_yuwan_enemy_encounters", "config_enable_yuwan_enemy_encounters", "启用本模组敌人", "控制 YuWanCard 的敌人遭遇是否会出现在对局中", 0,
            "YUWANCARD-ENABLE_YUWAN_ENEMY_ENCOUNTERS.title", "YUWANCARD-ENABLE_YUWAN_ENEMY_ENCOUNTERS.hover.desc"),
        new("EnableIgnisBossEncounter", ContentPageId, "enemy_encounters", "enable_ignis_boss_encounter", "config_enable_ignis_boss_encounter", "焰魔", "允许焰魔Boss遭遇出现在对局中", 1,
            "YUWANCARD-ENABLE_IGNIS_BOSS_ENCOUNTER.title", "YUWANCARD-ENABLE_IGNIS_BOSS_ENCOUNTER.hover.desc"),
        new("EnableKillerEliteEncounter", ContentPageId, "enemy_encounters", "enable_killer_elite_encounter", "config_enable_killer_elite_encounter", "杀手", "允许杀手精英遭遇出现在对局中", 2,
            "YUWANCARD-ENABLE_KILLER_ELITE_ENCOUNTER.title", "YUWANCARD-ENABLE_KILLER_ELITE_ENCOUNTER.hover.desc"),
        new("EnableYuWanEvents", ContentPageId, "events", "enable_yuwan_events", "config_enable_yuwan_events", "启用本模组事件", "控制 YuWanCard 的事件是否会出现在对局中", 0,
            "YUWANCARD-ENABLE_YUWAN_EVENTS.title", "YUWANCARD-ENABLE_YUWAN_EVENTS.hover.desc"),
        new("EnablePigPigAncient", ContentPageId, "ancients", "enable_pig_pig_ancient", "config_enable_pig_pig_ancient", "猪猪先古", "允许猪猪先古在巢穴开局中出现", 0,
            "YUWANCARD-ENABLE_PIG_PIG_ANCIENT.title", "YUWANCARD-ENABLE_PIG_PIG_ANCIENT.hover.desc"),
        new("EnableBlacksmithEvent", ContentPageId, "events", "enable_blacksmith_event", "config_enable_blacksmith_event", "铁匠铺", "允许铁匠铺事件出现在对局中", 1,
            "YUWANCARD-ENABLE_BLACKSMITH_EVENT.title", "YUWANCARD-ENABLE_BLACKSMITH_EVENT.hover.desc"),
        new("EnableHelloHumanEvent", ContentPageId, "events", "enable_hello_human_event", "config_enable_hello_human_event", "人，你好。", "允许“人，你好。”事件出现在对局中", 2,
            "YUWANCARD-ENABLE_HELLO_HUMAN_EVENT.title", "YUWANCARD-ENABLE_HELLO_HUMAN_EVENT.hover.desc"),
        new("EnableHorizonEvent", ContentPageId, "events", "enable_horizon_event", "config_enable_horizon_event", "天涯海角", "允许天涯海角事件出现在对局中", 3,
            "YUWANCARD-ENABLE_HORIZON_EVENT.title", "YUWANCARD-ENABLE_HORIZON_EVENT.hover.desc"),
        new("EnableSkullGoldRushEvent", ContentPageId, "events", "enable_skull_gold_rush_event", "config_enable_skull_gold_rush_event", "骷髅打金服", "允许骷髅打金服事件出现在对局中", 4,
            "YUWANCARD-ENABLE_SKULL_GOLD_RUSH_EVENT.title", "YUWANCARD-ENABLE_SKULL_GOLD_RUSH_EVENT.hover.desc"),
        new("EnableSunkenStatueQuestEvent", ContentPageId, "events", "enable_sunken_statue_quest_event", "config_enable_sunken_statue_quest_event", "沉没的石像", "允许沉没的石像事件出现在对局中", 5,
            "YUWANCARD-ENABLE_SUNKEN_STATUE_QUEST_EVENT.title", "YUWANCARD-ENABLE_SUNKEN_STATUE_QUEST_EVENT.hover.desc"),
        new("EnableZhiZhanZhiShangEvent", ContentPageId, "events", "enable_zhi_zhan_zhi_shang_event", "config_enable_zhi_zhan_zhi_shang_event", "止战之殇", "允许止战之殇事件出现在对局中", 6,
            "YUWANCARD-ENABLE_ZHI_ZHAN_ZHI_SHANG_EVENT.title", "YUWANCARD-ENABLE_ZHI_ZHAN_ZHI_SHANG_EVENT.hover.desc"),
    ];

    private static readonly SliderSettingDefinition[] SliderProps =
    [
        new("CursorScale", RootPageId, "display", "cursor_scale", "config_cursor_scale", "鼠标指针缩放",
            "自定义鼠标指针的大小，1.0x 约为原版的 64px", 0.1, 10.0, 0.1, "{0}x", 2,
            "YUWANCARD-CURSOR_SCALE.title", "YUWANCARD-CURSOR_SCALE.hover.desc"),
        new("PigBaseScale", RootPageId, "display", "pig_base_scale", "config_pig_base_scale", "猪角色体型大小",
            "猪角色在战斗中的基础体型倍率；开启血量缩放时会以这个倍率为基准变化", 0.1, 3.0, 0.1, "{0}x", 4,
            "YUWANCARD-PIG_BASE_SCALE.title", "YUWANCARD-PIG_BASE_SCALE.hover.desc"),
        new("BugPigDamageCap", RootPageId, "gameplay", "bug_pig_damage_cap", "config_bug_pig_damage_cap", "Bug猪伤害上限",
            "限制 Bug猪 根据日志 ERROR 数量计算后的最终伤害上限", 7.0, 999.0, 1.0, "{0}", 3,
            "YUWANCARD-BUG_PIG_DAMAGE_CAP.title", "YUWANCARD-BUG_PIG_DAMAGE_CAP.hover.desc"),
    ];

    private static readonly SubpageSettingDefinition[] SubpageProps =
    [
        new(RootPageId, "gameplay", "open_game_content_settings", ContentPageId, "OpenGameContentSettingsPage",
            "游戏内容设置", "打开游戏内容设置页面。", "打开", 100,
            "YUWANCARD-RITSU_OPEN_GAME_CONTENT.title", "YUWANCARD-RITSU_OPEN_GAME_CONTENT.desc", "YUWANCARD-RITSU_OPEN.button"),
        new(ContentPageId, "cards", "open_colorless_card_settings", ColorlessCardsPageId, "OpenColorlessCardSettingsPage",
            "无色卡牌设置", "使用画廊视图控制本模组新增无色卡牌是否会出现在对局中。", "打开", 0,
            "YUWANCARD-RITSU_OPEN_COLORLESS.title", "YUWANCARD-RITSU_OPEN_COLORLESS.desc", "YUWANCARD-RITSU_OPEN.button"),
    ];

    private static readonly CustomEntrySettingDefinition[] CustomEntryProps =
    [
        new(ColorlessCardsPageId, YuWanColorlessCardCatalog.SectionId, "colorless_card_gallery", "BuildColorlessCardGallery",
            "卡牌开关", "每行 5 个按钮；按钮文字为卡牌名，悬停可查看提示框。", 0,
            "YUWANCARD-RITSU_COLORLESS_GALLERY.title", "YUWANCARD-RITSU_COLORLESS_GALLERY.desc"),
    ];

    public static void TryDeferredRegister()
    {
        if (s_ritsuRegistered || MainFile.Config == null) return;

        if (IsRitsuLibAvailable())
            TryRegisterRitsuLib();
    }

    private static bool IsRitsuLibAvailable()
    {
        return ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsPageAttribute") != null;
    }

    // ── RitsuLib registration ─────────────────────────────

    private static bool TryRegisterRitsuLib()
    {
        if (s_ritsuRegistered) return true;

        try
        {
            var pageAttrType = ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsPageAttribute");
            var sectionAttrType = ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsSectionAttribute");
            var toggleAttrType = ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsToggleAttribute");
            var sliderAttrType = ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsSliderAttribute");
            var subpageAttrType = ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsSubpageAttribute");
            var customEntryAttrType = ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsCustomEntryAttribute");
            var bindingAttrType = ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsBindingAttribute");
            var bindingSourceType = ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsReflectionBindingSource");

            if (pageAttrType == null || sectionAttrType == null || toggleAttrType == null)
                return false;

            var dynamicTypes = CreateRitsuConfigTypes(pageAttrType, sectionAttrType, toggleAttrType, subpageAttrType,
                customEntryAttrType,
                sliderAttrType, bindingAttrType, bindingSourceType);
            if (dynamicTypes.Length == 0) return false;

            var frameworkType = ResolveTypeAcrossAssemblies("STS2RitsuLib.RitsuLibFramework");
            if (frameworkType == null)
            {
                MainFile.Logger.Warn("STS2RitsuLib.RitsuLibFramework type not found");
                return false;
            }

            var registerMethod = frameworkType.GetMethod("RegisterModSettingsReflectionProviderAndTryRegister",
                BindingFlags.Public | BindingFlags.Static, null, [typeof(Type)], null);
            if (registerMethod == null)
            {
                MainFile.Logger.Warn("RitsuLibFramework.RegisterModSettingsReflectionProviderAndTryRegister method not found");
                return false;
            }

            var pagesRegistered = 0;
            foreach (var dynamicType in dynamicTypes)
                pagesRegistered += registerMethod.Invoke(null, [dynamicType]) as int? ?? 0;

            MainFile.Logger.Info($"Registered {pagesRegistered} config page(s) via STS2-RitsuLib (direct reflection)");

            SyncRitsuLibToConfig();

            s_ritsuRegistered = true;
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to register config with STS2-RitsuLib: {ex.Message}");
            return false;
        }
    }

    // ── dynamic provider for RitsuLib ─────────────────────

    private static Type[] CreateRitsuConfigTypes(
        Type pageAttrType, Type sectionAttrType, Type toggleAttrType, Type? subpageAttrType,
        Type? customEntryAttrType, Type? sliderAttrType, Type? bindingAttrType, Type? bindingSourceType)
    {
        try
        {
            if (s_dynamicRitsuProviderTypes != null)
                return s_dynamicRitsuProviderTypes;

            var pageCtor = pageAttrType.GetConstructor([typeof(string), typeof(string)]);
            if (pageCtor == null) return [];

            var sectionCtor = sectionAttrType.GetConstructor([typeof(string)]);

            var toggleCtor = toggleAttrType.GetConstructor([typeof(string), typeof(string)]);
            if (toggleCtor == null) return [];

            var subpageCtor = subpageAttrType?.GetConstructor([typeof(string), typeof(string), typeof(string)]);
            var subpageLabelProp = subpageAttrType?.GetProperty("Label");
            var subpageDescProp = subpageAttrType?.GetProperty("Description");
            var subpageOrderProp = subpageAttrType?.GetProperty("Order");
            var subpageButtonTextProp = subpageAttrType?.GetProperty("ButtonText");
            var customEntryCtor = customEntryAttrType?.GetConstructor([typeof(string), typeof(string)]);
            var customEntryLabelProp = customEntryAttrType?.GetProperty("Label");
            var customEntryDescProp = customEntryAttrType?.GetProperty("Description");
            var customEntryOrderProp = customEntryAttrType?.GetProperty("Order");

            var labelProp = toggleAttrType.GetProperty("Label");
            var descProp = toggleAttrType.GetProperty("Description");
            var orderProp = toggleAttrType.GetProperty("Order");

            var asmName = new AssemblyName("YuWanCard.DynamicRitsuConfig");
            var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            var modBuilder = asmBuilder.DefineDynamicModule("RitsuModule");
            var sliderCtor = sliderAttrType?.GetConstructor(
                [typeof(string), typeof(string), typeof(double), typeof(double), typeof(double)]);
            var sliderLabelProp = sliderAttrType?.GetProperty("Label");
            var sliderDescProp = sliderAttrType?.GetProperty("Description");
            var sliderOrderProp = sliderAttrType?.GetProperty("Order");

            var providerTypes = new List<Type>(RitsuPages.Length);
            foreach (var page in RitsuPages)
            {
                var typeBuilder = modBuilder.DefineType(
                    $"YuWanCard.Config.{page.TypeName}",
                    TypeAttributes.Public | TypeAttributes.Class);

                ApplyRitsuPageAttribute(typeBuilder, pageAttrType, pageCtor, page);
                ApplyRitsuSectionAttributes(typeBuilder, sectionAttrType, sectionCtor, page.PageId);

                foreach (var t in ToggleProps.Where(p => string.Equals(p.RitsuPageId, page.PageId, StringComparison.Ordinal)))
                    EmitRitsuBoolProperty(typeBuilder, toggleCtor, labelProp, descProp, orderProp,
                        bindingAttrType, bindingSourceType, t);

                if (sliderCtor != null)
                    foreach (var s in SliderProps.Where(p => string.Equals(p.RitsuPageId, page.PageId, StringComparison.Ordinal)))
                        EmitRitsuDoubleProperty(typeBuilder, sliderCtor, sliderLabelProp, sliderDescProp, sliderOrderProp,
                            bindingAttrType, bindingSourceType, s);

                if (subpageCtor != null)
                    foreach (var s in SubpageProps.Where(p => string.Equals(p.RitsuPageId, page.PageId, StringComparison.Ordinal)))
                        EmitRitsuSubpageMethod(typeBuilder, subpageCtor, subpageLabelProp, subpageDescProp,
                            subpageOrderProp, subpageButtonTextProp, s);

                if (customEntryCtor != null)
                    foreach (var s in CustomEntryProps.Where(p => string.Equals(p.RitsuPageId, page.PageId, StringComparison.Ordinal)))
                        EmitRitsuCustomEntryMethod(typeBuilder, customEntryCtor, customEntryLabelProp, customEntryDescProp,
                            customEntryOrderProp, s);

                if (typeBuilder.CreateType() is { } createdType)
                    providerTypes.Add(createdType);
            }

            s_dynamicRitsuProviderTypes = [.. providerTypes];
            return s_dynamicRitsuProviderTypes;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to create dynamic Ritsu config type: {ex.Message}");
            return [];
        }
    }

    private static void ApplyRitsuPageAttribute(
        TypeBuilder typeBuilder,
        Type pageAttrType,
        ConstructorInfo pageCtor,
        ConfigPageDefinition page)
    {
        var props = new List<PropertyInfo>();
        var values = new List<object>();

        AddNamedAttrValue(pageAttrType, props, values, "Title", page.Title);
        AddNamedAttrValue(pageAttrType, props, values, "Description", page.Description);
        AddNamedAttrValue(pageAttrType, props, values, "ModDisplayName", page.ModDisplayName);
        AddNamedAttrValue(pageAttrType, props, values, "ParentPageId", page.ParentPageId);
        AddNamedAttrValue(pageAttrType, props, values, "SortOrder", page.SortOrder);
        AddNamedAttrValue(pageAttrType, props, values, "TitleLocTable", "settings_ui");
        AddNamedAttrValue(pageAttrType, props, values, "TitleLocKey", page.TitleLocKey);
        AddNamedAttrValue(pageAttrType, props, values, "DescriptionLocTable", "settings_ui");
        AddNamedAttrValue(pageAttrType, props, values, "DescriptionLocKey", page.DescriptionLocKey);

        typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
            pageCtor,
            [ModId, page.PageId],
            props.ToArray(),
            values.ToArray()));
    }

    private static void ApplyRitsuSectionAttributes(
        TypeBuilder typeBuilder,
        Type sectionAttrType,
        ConstructorInfo? sectionCtor,
        string pageId)
    {
        if (sectionCtor == null)
            return;

        foreach (var section in RitsuSections
                     .Where(s => string.Equals(s.PageId, pageId, StringComparison.Ordinal))
                     .OrderBy(s => s.SortOrder))
        {
            var props = new List<PropertyInfo>();
            var values = new List<object>();

            AddNamedAttrValue(sectionAttrType, props, values, "Title", section.Title);
            AddNamedAttrValue(sectionAttrType, props, values, "Description", section.Description);
            AddNamedAttrValue(sectionAttrType, props, values, "SortOrder", section.SortOrder);
            AddNamedAttrValue(sectionAttrType, props, values, "TitleLocTable", "settings_ui");
            AddNamedAttrValue(sectionAttrType, props, values, "TitleLocKey", section.TitleLocKey);
            AddNamedAttrValue(sectionAttrType, props, values, "DescriptionLocTable", "settings_ui");
            AddNamedAttrValue(sectionAttrType, props, values, "DescriptionLocKey", section.DescriptionLocKey);

            typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                sectionCtor,
                [section.SectionId],
                props.ToArray(),
                values.ToArray()));
        }
    }

    private static void EmitRitsuBoolProperty(
        TypeBuilder typeBuilder, ConstructorInfo toggleCtor,
        PropertyInfo? labelProp, PropertyInfo? descProp, PropertyInfo? orderProp,
        Type? bindingAttrType, Type? bindingSourceType,
        ToggleSettingDefinition setting)
    {
        var prop = typeBuilder.DefineProperty(setting.PropertyName, PropertyAttributes.None, typeof(bool), null);

        prop.SetCustomAttribute(BuildEntryAttribute(
            toggleCtor, [setting.RitsuId, setting.RitsuSectionId], labelProp, descProp, orderProp,
            setting.Label, setting.Description, setting.Order, setting.LabelLocKey, setting.DescriptionLocKey));

        if (TryCreateRitsuBindingAttribute(bindingAttrType, bindingSourceType, setting.DataKey) is { } bindingAttrBuilder)
            prop.SetCustomAttribute(bindingAttrBuilder);

        var getter = typeBuilder.DefineMethod(
            $"get_{setting.PropertyName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            typeof(bool), Type.EmptyTypes);
        var getIL = getter.GetILGenerator();
        getIL.Emit(OpCodes.Ldstr, setting.PropertyName);
        getIL.Emit(OpCodes.Call, typeof(RitsuConfigRuntimeBridge).GetMethod(
            nameof(RitsuConfigRuntimeBridge.ReadRuntimeBool),
            BindingFlags.Public | BindingFlags.Static)!);
        getIL.Emit(OpCodes.Ret);
        prop.SetGetMethod(getter);

        var setter = typeBuilder.DefineMethod(
            $"set_{setting.PropertyName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            null, [typeof(bool)]);
        var setIL = setter.GetILGenerator();
        setIL.Emit(OpCodes.Ldstr, setting.PropertyName);
        setIL.Emit(OpCodes.Ldarg_1);
        setIL.Emit(OpCodes.Call, typeof(RitsuConfigRuntimeBridge).GetMethod(
            nameof(RitsuConfigRuntimeBridge.ApplyRuntimeBool),
            BindingFlags.Public | BindingFlags.Static)!);
        setIL.Emit(OpCodes.Ret);
        prop.SetSetMethod(setter);
    }

    private static void EmitRitsuDoubleProperty(
        TypeBuilder typeBuilder, ConstructorInfo sliderCtor,
        PropertyInfo? labelProp, PropertyInfo? descProp, PropertyInfo? orderProp,
        Type? bindingAttrType, Type? bindingSourceType,
        SliderSettingDefinition setting)
    {
        var prop = typeBuilder.DefineProperty(setting.PropertyName, PropertyAttributes.None, typeof(double), null);

        prop.SetCustomAttribute(BuildEntryAttribute(
            sliderCtor, [setting.RitsuId, setting.RitsuSectionId, setting.Min, setting.Max, setting.Step], labelProp, descProp, orderProp,
            setting.Label, setting.Description, setting.Order, setting.LabelLocKey, setting.DescriptionLocKey));

        if (TryCreateRitsuBindingAttribute(bindingAttrType, bindingSourceType, setting.DataKey) is { } bindingAttrBuilder)
            prop.SetCustomAttribute(bindingAttrBuilder);

        var getter = typeBuilder.DefineMethod(
            $"get_{setting.PropertyName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            typeof(double), Type.EmptyTypes);
        var getIL = getter.GetILGenerator();
        getIL.Emit(OpCodes.Ldstr, setting.PropertyName);
        getIL.Emit(OpCodes.Call, typeof(RitsuConfigRuntimeBridge).GetMethod(
            nameof(RitsuConfigRuntimeBridge.ReadRuntimeDouble),
            BindingFlags.Public | BindingFlags.Static)!);
        getIL.Emit(OpCodes.Ret);
        prop.SetGetMethod(getter);

        var setter = typeBuilder.DefineMethod(
            $"set_{setting.PropertyName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            null, [typeof(double)]);
        var setIL = setter.GetILGenerator();
        setIL.Emit(OpCodes.Ldstr, setting.PropertyName);
        setIL.Emit(OpCodes.Ldarg_1);
        setIL.Emit(OpCodes.Call, typeof(RitsuConfigRuntimeBridge).GetMethod(
            nameof(RitsuConfigRuntimeBridge.ApplyRuntimeDouble),
            BindingFlags.Public | BindingFlags.Static)!);
        setIL.Emit(OpCodes.Ret);
        prop.SetSetMethod(setter);
    }

    private static void EmitRitsuSubpageMethod(
        TypeBuilder typeBuilder,
        ConstructorInfo subpageCtor,
        PropertyInfo? labelProp,
        PropertyInfo? descProp,
        PropertyInfo? orderProp,
        PropertyInfo? buttonTextProp,
        SubpageSettingDefinition setting)
    {
        var method = typeBuilder.DefineMethod(
            setting.MethodName,
            MethodAttributes.Public | MethodAttributes.Static,
            typeof(void),
            Type.EmptyTypes);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ret);

        var props = new List<PropertyInfo>();
        var values = new List<object>();

        if (labelProp != null)
        {
            props.Add(labelProp);
            values.Add(setting.Label);
        }

        if (descProp != null && setting.Description != null)
        {
            props.Add(descProp);
            values.Add(setting.Description);
        }

        AddOptionalLocProperty(subpageCtor.DeclaringType, props, values, "LabelLocTable", "settings_ui");
        AddOptionalLocProperty(subpageCtor.DeclaringType, props, values, "LabelLocKey", setting.LabelLocKey);
        AddOptionalLocProperty(subpageCtor.DeclaringType, props, values, "DescriptionLocTable", "settings_ui");
        AddOptionalLocProperty(subpageCtor.DeclaringType, props, values, "DescriptionLocKey", setting.DescriptionLocKey);

        if (buttonTextProp != null && setting.ButtonText != null)
        {
            props.Add(buttonTextProp);
            values.Add(setting.ButtonText);
        }

        AddOptionalLocProperty(subpageCtor.DeclaringType, props, values, "ButtonTextLocTable", "settings_ui");
        AddOptionalLocProperty(subpageCtor.DeclaringType, props, values, "ButtonTextLocKey", setting.ButtonTextLocKey);

        if (orderProp != null)
        {
            props.Add(orderProp);
            values.Add(setting.Order);
        }

        method.SetCustomAttribute(new CustomAttributeBuilder(
            subpageCtor,
            [setting.EntryId, setting.RitsuSectionId, setting.TargetPageId],
            props.ToArray(),
            values.ToArray()));
    }

    private static void EmitRitsuCustomEntryMethod(
        TypeBuilder typeBuilder,
        ConstructorInfo customEntryCtor,
        PropertyInfo? labelProp,
        PropertyInfo? descProp,
        PropertyInfo? orderProp,
        CustomEntrySettingDefinition setting)
    {
        var method = typeBuilder.DefineMethod(
            setting.MethodName,
            MethodAttributes.Public | MethodAttributes.Static,
            typeof(Control),
            Type.EmptyTypes);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Call, typeof(RitsuConfigRuntimeBridge).GetMethod(
            nameof(RitsuConfigRuntimeBridge.CreateColorlessCardSettingsControl),
            BindingFlags.Public | BindingFlags.Static)!);
        il.Emit(OpCodes.Ret);

        method.SetCustomAttribute(BuildEntryAttribute(
            customEntryCtor,
            [setting.EntryId, setting.RitsuSectionId],
            labelProp,
            descProp,
            orderProp,
            setting.Label,
            setting.Description,
            setting.Order,
            setting.LabelLocKey,
            setting.DescriptionLocKey));
    }

    // ── sync ─────────────────────────────────────────────

    private static void SyncRitsuLibToConfig()
    {
        try
        {
            var frameworkType = ResolveTypeAcrossAssemblies("STS2RitsuLib.RitsuLibFramework");
            if (frameworkType == null) return;

            var getDataStoreMethod = frameworkType.GetMethod("GetDataStore",
                BindingFlags.Public | BindingFlags.Static, null, [typeof(string)], null);
            if (getDataStoreMethod == null) return;

            var dataStore = getDataStoreMethod.Invoke(null, [ModId]);
            if (dataStore == null) return;

            var initGlobalMethod = dataStore.GetType().GetMethod("InitializeGlobal");
            initGlobalMethod?.Invoke(dataStore, null);

            var ritsuAsm = dataStore.GetType().Assembly;
            var mirrorSourceType = ritsuAsm.GetType("STS2RitsuLib.Settings.RuntimeReflectionMirrorSource");
            var boxOpenType = mirrorSourceType?.GetNestedType("ReflectionBindingBox`1", BindingFlags.NonPublic);
            if (boxOpenType == null) return;

            var getMethod = dataStore.GetType().GetMethod("Get", [typeof(string)]);
            if (getMethod == null) return;

            foreach (var t in ToggleProps)
                SyncOneValue<bool>(dataStore, getMethod, boxOpenType, t.PropertyName,
                    v => SetConfigBool(t.PropertyName, v));

            foreach (var s in SliderProps)
                SyncOneValue<double>(dataStore, getMethod, boxOpenType, s.PropertyName,
                    v => SetConfigDouble(s.PropertyName, v));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to sync RitsuLib config to YuWanCardConfig: {ex.Message}");
        }
    }

    private static void SyncOneValue<T>(object dataStore, MethodInfo getMethod, Type boxOpenType,
        string propName, Action<T> apply)
    {
        var boxType = boxOpenType.MakeGenericType(typeof(T));
        var getTyped = getMethod.MakeGenericMethod(boxType);
        var valueProp = boxType.GetProperty("Value");
        if (valueProp == null) return;

        foreach (var dataKey in EnumerateRitsuDataKeys(propName))
        {
            try
            {
                var box = getTyped.Invoke(dataStore, [dataKey]);
                if (box == null) continue;

                apply((T)valueProp.GetValue(box)!);
                break;
            }
            catch
            {
                // Key may not exist yet (first run) or use a legacy format.
            }
        }
    }

    // ── helpers ─────────────────────────────────────────

    /// <summary>Builds a RitsuLib ordered-entry attribute (toggle/slider), setting Label, Description,
    /// and Order named properties when available on the attribute type.</summary>
    private static CustomAttributeBuilder BuildEntryAttribute(
        ConstructorInfo ctor, object[] ctorArgs,
        PropertyInfo? labelProp, PropertyInfo? descProp, PropertyInfo? orderProp,
        string label, string? description, int order,
        string? labelLocKey = null, string? descriptionLocKey = null)
    {
        var props = new List<PropertyInfo>();
        var values = new List<object>();

        if (labelProp != null) { props.Add(labelProp); values.Add(label); }
        if (descProp != null && description != null) { props.Add(descProp); values.Add(description); }
        if (orderProp != null) { props.Add(orderProp); values.Add(order); }
        AddOptionalLocProperty(ctor.DeclaringType, props, values, "LabelLocTable", "settings_ui");
        AddOptionalLocProperty(ctor.DeclaringType, props, values, "LabelLocKey", labelLocKey);
        AddOptionalLocProperty(ctor.DeclaringType, props, values, "DescriptionLocTable", "settings_ui");
        AddOptionalLocProperty(ctor.DeclaringType, props, values, "DescriptionLocKey", descriptionLocKey);

        return new CustomAttributeBuilder(ctor, ctorArgs, props.ToArray(), values.ToArray());
    }

    private static void AddNamedAttrValue(
        Type attributeType,
        List<PropertyInfo> props,
        List<object> values,
        string propertyName,
        object? value)
    {
        if (value == null)
            return;

        if (attributeType.GetProperty(propertyName) is { } prop)
        {
            props.Add(prop);
            values.Add(value);
        }
    }

    private static void AddOptionalLocProperty(
        Type? attributeType,
        List<PropertyInfo> props,
        List<object> values,
        string propertyName,
        object? value)
    {
        if (attributeType == null || value == null)
        {
            return;
        }

        if (attributeType.GetProperty(propertyName) is { } prop)
        {
            props.Add(prop);
            values.Add(value);
        }
    }

    private static CustomAttributeBuilder? TryCreateRitsuBindingAttribute(
        Type? bindingAttrType, Type? bindingSourceType, string dataKey)
    {
        if (bindingAttrType == null || bindingSourceType == null)
            return null;

        var bindingCtor = bindingAttrType.GetConstructor(Type.EmptyTypes);
        var sourceProp = bindingAttrType.GetProperty("Source");
        var dataKeyProp = bindingAttrType.GetProperty("DataKey");
        if (bindingCtor == null || sourceProp == null || dataKeyProp == null)
            return null;

        var globalVal = Enum.Parse(bindingSourceType, "Global");
        return new CustomAttributeBuilder(
            bindingCtor, Array.Empty<object>(),
            [sourceProp, dataKeyProp], [globalVal, dataKey]);
    }

    private static IEnumerable<string> EnumerateRitsuDataKeys(string propertyName)
    {
        if (TryGetRitsuDataKey(propertyName, out var dataKey))
            yield return dataKey;

        yield return $"reflect::YuWanCard.Config.YuWanCardRitsuConfigProvider.{propertyName}";
    }

    private static bool TryGetRitsuDataKey(string propertyName, out string dataKey)
    {
        foreach (var t in ToggleProps)
        {
            if (string.Equals(t.PropertyName, propertyName, StringComparison.Ordinal))
            {
                dataKey = t.DataKey;
                return true;
            }
        }

        foreach (var s in SliderProps)
        {
            if (string.Equals(s.PropertyName, propertyName, StringComparison.Ordinal))
            {
                dataKey = s.DataKey;
                return true;
            }
        }

        dataKey = string.Empty;
        return false;
    }

    private static bool GetConfigBool(string name)
    {
        return (bool)typeof(Config.YuWanCardConfig).GetProperty(name)!.GetValue(null)!;
    }

    private static void SetConfigBool(string name, bool value)
    {
        typeof(Config.YuWanCardConfig).GetProperty(name)?.SetValue(null, value);
    }

    private static double GetConfigDouble(string name)
    {
        return (double)typeof(Config.YuWanCardConfig).GetProperty(name)!.GetValue(null)!;
    }

    private static void SetConfigDouble(string name, double value)
    {
        typeof(Config.YuWanCardConfig).GetProperty(name)?.SetValue(null, value);
    }

    private static Type? ResolveTypeAcrossAssemblies(string fullName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var type = assembly.GetType(fullName, false);
                if (type != null) return type;
            }
            catch { }
        }
        return null;
    }
}

public static class RitsuConfigRuntimeBridge
{
    private static readonly Color EnabledButtonColor = new("4D8B31");
    private static readonly Color DisabledButtonColor = new("6B2F2F");
    private static readonly Color SummaryTextColor = new(0.9f, 0.9f, 0.9f);
    private const string SettingsLocTable = "settings_ui";

    public static bool ReadRuntimeBool(string propertyName)
    {
        return (bool)typeof(Config.YuWanCardConfig).GetProperty(propertyName)!.GetValue(null)!;
    }

    public static void ApplyRuntimeBool(string propertyName, bool value)
    {
        typeof(Config.YuWanCardConfig).GetProperty(propertyName)?.SetValue(null, value);

        if (propertyName == nameof(Config.YuWanCardConfig.EnableCustomCursor))
            Patches.CursorReplacePatch.RefreshCursor();
    }

    public static double ReadRuntimeDouble(string propertyName)
    {
        return (double)typeof(Config.YuWanCardConfig).GetProperty(propertyName)!.GetValue(null)!;
    }

    public static void ApplyRuntimeDouble(string propertyName, double value)
    {
        typeof(Config.YuWanCardConfig).GetProperty(propertyName)?.SetValue(null, value);

        if (propertyName == nameof(Config.YuWanCardConfig.CursorScale))
            Patches.CursorReplacePatch.RefreshCursor();
    }

    public static Control CreateColorlessCardSettingsControl()
    {
        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = "PanelContainer"
        };

        var description = new Godot.Label
        {
            Text = GetSettingsLocText("YUWANCARD-RITSU_COLORLESS_CONTROL.desc"),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        description.AddThemeColorOverride("font_color", SummaryTextColor);
        root.AddChild(description);

        var toolbar = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        root.AddChild(toolbar);

        var summaryLabel = new Godot.Label
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        summaryLabel.AddThemeColorOverride("font_color", SummaryTextColor);

        var grid = new GridContainer
        {
            Columns = YuWanColorlessCardCatalog.ButtonsPerRow,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };

        var buttonsByKey = new Dictionary<string, Button>(StringComparer.Ordinal);

        Button CreateActionButton(string text, Action onPressed)
        {
            var button = new Button
            {
                Text = text,
                CustomMinimumSize = new Vector2(120f, 42f),
                FocusMode = Control.FocusModeEnum.None
            };
            button.Pressed += onPressed;
            return button;
        }

        void RefreshSummary()
        {
            int enabledCount = buttonsByKey.Values.Count(static button => button.ButtonPressed);
            summaryLabel.Text = string.Format(GetSettingsLocText("YUWANCARD-RITSU_COLORLESS_SUMMARY.text"), enabledCount, YuWanColorlessCardCatalog.Cards.Count);
        }

        void RefreshButtonVisual(Button button)
        {
            button.Modulate = button.ButtonPressed ? Colors.White : new Color(0.78f, 0.78f, 0.78f, 0.95f);

            var normalStyle = new StyleBoxFlat
            {
                BgColor = button.ButtonPressed ? EnabledButtonColor : DisabledButtonColor,
                BorderWidthBottom = 2,
                BorderWidthLeft = 2,
                BorderWidthRight = 2,
                BorderWidthTop = 2,
                BorderColor = new Color(0f, 0f, 0f, 0.35f),
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8,
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8
            };

            var hoverStyle = (StyleBoxFlat)normalStyle.Duplicate();
            hoverStyle.BgColor = button.ButtonPressed
                ? EnabledButtonColor.Lightened(0.12f)
                : DisabledButtonColor.Lightened(0.12f);

            var pressedStyle = (StyleBoxFlat)normalStyle.Duplicate();
            pressedStyle.BgColor = button.ButtonPressed
                ? EnabledButtonColor.Darkened(0.08f)
                : DisabledButtonColor.Darkened(0.08f);

            button.AddThemeStyleboxOverride("normal", normalStyle);
            button.AddThemeStyleboxOverride("hover", hoverStyle);
            button.AddThemeStyleboxOverride("pressed", pressedStyle);
            button.AddThemeStyleboxOverride("focus", hoverStyle);
        }

        void RefreshAllButtons()
        {
            foreach (var button in buttonsByKey.Values)
            {
                RefreshButtonVisual(button);
            }

            RefreshSummary();
        }

        toolbar.AddChild(CreateActionButton(GetSettingsLocText("YUWANCARD-RITSU_COLORLESS_ENABLE_ALL.button"), () =>
        {
            if (!YuWanColorlessCardSettings.SetAll(true))
            {
                return;
            }

            foreach (var button in buttonsByKey.Values)
            {
                button.SetPressedNoSignal(true);
            }

            RefreshAllButtons();
        }));

        toolbar.AddChild(CreateActionButton(GetSettingsLocText("YUWANCARD-RITSU_COLORLESS_DISABLE_ALL.button"), () =>
        {
            if (!YuWanColorlessCardSettings.SetAll(false))
            {
                return;
            }

            foreach (var button in buttonsByKey.Values)
            {
                button.SetPressedNoSignal(false);
            }

            RefreshAllButtons();
        }));

        toolbar.AddChild(CreateActionButton(GetSettingsLocText("YUWANCARD-RITSU_COLORLESS_INVERT.button"), () =>
        {
            bool changed = false;
            foreach (var (key, button) in buttonsByKey)
            {
                bool nextValue = !button.ButtonPressed;
                changed |= YuWanColorlessCardSettings.SetEnabled(key, nextValue);
                button.SetPressedNoSignal(nextValue);
            }

            if (changed)
            {
                RefreshAllButtons();
            }
        }));

        toolbar.AddChild(summaryLabel);

        foreach (var definition in YuWanColorlessCardCatalog.Cards)
        {
            CardModel canonicalCard = YuWanColorlessCardCatalog.CreateCanonicalCard(definition);
            string label = canonicalCard.Title;
            bool enabled = YuWanColorlessCardSettings.IsEnabled(definition.CardType);

            var button = new Button
            {
                Text = label,
                ToggleMode = true,
                ButtonPressed = enabled,
                FocusMode = Control.FocusModeEnum.None,
                CustomMinimumSize = new Vector2(0f, 52f),
                ClipText = true,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };

            button.Toggled += isPressed =>
            {
                YuWanColorlessCardSettings.SetEnabled(definition.Key, isPressed);
                RefreshButtonVisual(button);
                RefreshSummary();
            };

            button.MouseEntered += () =>
            {
                var hoverTips = BuildColorlessCardHoverTips(canonicalCard);
                if (!hoverTips.Any())
                {
                    return;
                }

                NHoverTipSet.Remove(button);
                var alignment = HoverTip.GetHoverTipAlignment(button);
                var tipSet = NHoverTipSet.CreateAndShow(button, hoverTips, alignment);
                if (tipSet != null)
                {
                    PositionColorlessCardHoverTip(button, tipSet, alignment);
                }
            };

            button.MouseExited += () => NHoverTipSet.Remove(button);
            button.TreeExiting += () => NHoverTipSet.Remove(button);

            buttonsByKey[definition.Key] = button;
            RefreshButtonVisual(button);
            grid.AddChild(button);
        }

        RefreshSummary();
        root.AddChild(grid);
        return root;
    }

    private static IReadOnlyList<IHoverTip> BuildColorlessCardHoverTips(CardModel canonicalCard)
    {
        return
        [
            HoverTipFactory.FromCard(canonicalCard, canonicalCard.IsUpgraded),
            .. canonicalCard.HoverTips
        ];
    }

    private static void PositionColorlessCardHoverTip(Button button, NHoverTipSet tipSet, HoverTipAlignment alignment)
    {
        if (alignment != HoverTipAlignment.Right)
        {
            return;
        }

        var cardContainer = tipSet.GetNodeOrNull<Control>("cardHoverTipContainer");
        var textContainer = tipSet.GetNodeOrNull<Control>("textHoverTipContainer");
        if (cardContainer == null || textContainer == null)
        {
            return;
        }

        var anchor = button.GlobalPosition + new Vector2(button.Size.X + 12f, 0f);
        cardContainer.GlobalPosition = anchor;
        textContainer.GlobalPosition = anchor + new Vector2(cardContainer.Size.X + 18f, 0f);
    }

    private static string GetSettingsLocText(string key)
    {
        return new LocString(SettingsLocTable, key).GetRawText();
    }
}
