using System.Reflection;
using System.Reflection.Emit;

namespace YuWanCard;

/// <summary>
/// Registers YuWanCard settings with either BaseLib or STS2-RitsuLib via runtime
/// reflection-emitted adapter/provider types, avoiding compile-time dependencies on
/// either library. BaseLib is tried first, then RitsuLib. Supports boolean toggles
/// and double sliders.
/// </summary>
internal static class ConfigRegistrar
{
    private const string ModId = MainFile.ModId;
    private const string RootPageId = "yuwan_card";
    private const string ContentPageId = "game_content";

    private static bool s_registered;
    private static bool s_ritsuRegistered;

    private static Type? s_dynamicAdapterType;
    private static object? s_dynamicAdapterInstance;
    private static Type[]? s_dynamicRitsuProviderTypes;

    private sealed record ConfigPageDefinition(
        string TypeName,
        string PageId,
        string Title,
        string? Description,
        int SortOrder,
        string? ParentPageId = null,
        string? ModDisplayName = null);

    private sealed record ConfigSectionDefinition(
        string PageId,
        string SectionId,
        string Title,
        string? Description,
        int SortOrder);

    private sealed record ToggleSettingDefinition(
        string PropertyName,
        string BaseLibSection,
        string RitsuPageId,
        string RitsuSectionId,
        string RitsuId,
        string DataKey,
        string Label,
        string? Description,
        int Order);

    private sealed record SliderSettingDefinition(
        string PropertyName,
        string BaseLibSection,
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
        int Order);

    private sealed record SubpageSettingDefinition(
        string RitsuPageId,
        string RitsuSectionId,
        string EntryId,
        string TargetPageId,
        string MethodName,
        string Label,
        string? Description,
        string? ButtonText,
        int Order);

    private static readonly ConfigPageDefinition[] RitsuPages =
    [
        new("YuWanCardRitsuConfigProvider", RootPageId, "YuWanCard 设置", null, 0, null, "YuWanCard"),
        new("YuWanCardRitsuContentConfigProvider", ContentPageId, "游戏内容设置", "控制本模组敌人和事件是否会出现在对局中", 100, RootPageId, "YuWanCard"),
    ];

    private static readonly ConfigSectionDefinition[] RitsuSections =
    [
        new(RootPageId, "display", "显示设置", null, 0),
        new(RootPageId, "multiplayer", "多人游戏设置", null, 100),
        new(RootPageId, "updates", "更新设置", null, 200),
        new(RootPageId, "gameplay", "游戏设置", null, 300),
        new(ContentPageId, "enemy_encounters", "敌人遭遇", null, 0),
        new(ContentPageId, "events", "事件", null, 100),
    ];

    // Boolean toggle settings. BaseLib and RitsuLib use independent grouping metadata.
    private static readonly ToggleSettingDefinition[] ToggleProps =
    [
        new("EnableDeathEffect", "显示设置", RootPageId, "display", "enable_death_effect", "config_enable_death_effect", "死亡特效", "击败敌人时显示死亡特效", 0),
        new("EnableCustomCursor", "显示设置", RootPageId, "display", "enable_custom_cursor", "config_enable_custom_cursor", "自定义鼠标指针", "用猪猪主题指针替换游戏默认鼠标指针", 1),
        new("BypassModelDbHashCheck", "多人游戏设置", RootPageId, "multiplayer", "bypass_modeldb_check", "config_bypass_modeldb_hash_check", "跳过哈希检查", "多人模式下跳过ModelDb哈希校验", 0),
        new("EnableAutoUpdateCheck", "更新设置", RootPageId, "updates", "enable_auto_update", "config_enable_auto_update_check", "自动检查更新", "启动时自动检查模组更新", 0),
        new("EnableSevenCursesRing", "游戏设置", RootPageId, "gameplay", "enable_seven_curses_ring", "config_enable_seven_curses_ring", "七咒之戒", "在Neow处可选择七咒之戒", 0),
        new("EnableMaliceSelection", "游戏设置", RootPageId, "gameplay", "enable_malice_selection", "config_enable_malice_selection", "恶意难度选择", "在角色选择界面显示恶意难度选择面板", 1),
        new("EnableYuWanEnemyEncounters", "游戏内容设置", ContentPageId, "enemy_encounters", "enable_yuwan_enemy_encounters", "config_enable_yuwan_enemy_encounters", "启用本模组敌人", "控制 YuWanCard 的敌人遭遇是否会出现在对局中", 0),
        new("EnableIgnisBossEncounter", "游戏内容设置", ContentPageId, "enemy_encounters", "enable_ignis_boss_encounter", "config_enable_ignis_boss_encounter", "焰魔", "允许焰魔首领遭遇出现在对局中", 1),
        new("EnableKillerEliteEncounter", "游戏内容设置", ContentPageId, "enemy_encounters", "enable_killer_elite_encounter", "config_enable_killer_elite_encounter", "杀手", "允许杀手精英遭遇出现在对局中", 2),
        new("EnableYuWanEvents", "游戏内容设置", ContentPageId, "events", "enable_yuwan_events", "config_enable_yuwan_events", "启用本模组事件", "控制 YuWanCard 的事件是否会出现在对局中", 0),
        new("EnableBlacksmithEvent", "游戏内容设置", ContentPageId, "events", "enable_blacksmith_event", "config_enable_blacksmith_event", "铁匠铺", "允许铁匠铺事件出现在对局中", 1),
        new("EnableHelloHumanEvent", "游戏内容设置", ContentPageId, "events", "enable_hello_human_event", "config_enable_hello_human_event", "人，你好。", "允许“人，你好。”事件出现在对局中", 2),
        new("EnableHorizonEvent", "游戏内容设置", ContentPageId, "events", "enable_horizon_event", "config_enable_horizon_event", "天涯海角", "允许天涯海角事件出现在对局中", 3),
        new("EnableSkullGoldRushEvent", "游戏内容设置", ContentPageId, "events", "enable_skull_gold_rush_event", "config_enable_skull_gold_rush_event", "骷髅打金服", "允许骷髅打金服事件出现在对局中", 4),
        new("EnableSunkenStatueQuestEvent", "游戏内容设置", ContentPageId, "events", "enable_sunken_statue_quest_event", "config_enable_sunken_statue_quest_event", "沉没的石像", "允许沉没的石像事件出现在对局中", 5),
        new("EnableZhiZhanZhiShangEvent", "游戏内容设置", ContentPageId, "events", "enable_zhi_zhan_zhi_shang_event", "config_enable_zhi_zhan_zhi_shang_event", "止战之殇", "允许止战之殇事件出现在对局中", 6),
    ];

    private static readonly SliderSettingDefinition[] SliderProps =
    [
        new("CursorScale", "显示设置", RootPageId, "display", "cursor_scale", "config_cursor_scale", "鼠标指针缩放",
            "自定义鼠标指针的大小，1.0x 约为原版的 64px", 0.1, 10.0, 0.1, "{0}x", 2),
    ];

    private static readonly SubpageSettingDefinition[] SubpageProps =
    [
        new(RootPageId, "gameplay", "open_game_content_settings", ContentPageId, "OpenGameContentSettingsPage",
            "游戏内容设置", "打开游戏内容设置页面。", "打开", 100),
    ];

    public static void TryDeferredRegister()
    {
        if (s_registered || MainFile.Config == null) return;

        if (IsBaseLibAvailable() && TryRegisterBaseLibDirect())
            return;

        if (IsRitsuLibAvailable())
            TryRegisterRitsuLib();
    }

    private static bool IsBaseLibAvailable()
    {
        return ResolveTypeAcrossAssemblies("BaseLib.Config.SimpleModConfig") != null;
    }

    private static bool IsRitsuLibAvailable()
    {
        return ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsPageAttribute") != null;
    }

    // ── BaseLib registration ──────────────────────────────

    private static bool TryRegisterBaseLibDirect()
    {
        try
        {
            var adapter = CreateDynamicAdapter();
            if (adapter == null) return false;

            var registryType = ResolveTypeAcrossAssemblies("BaseLib.Config.ModConfigRegistry");
            var modConfigType = ResolveTypeAcrossAssemblies("BaseLib.Config.ModConfig");
            if (registryType == null || modConfigType == null)
            {
                MainFile.Logger.Warn("BaseLib ModConfigRegistry or ModConfig type not found");
                return false;
            }

            var registerMethod = registryType.GetMethod("Register", [typeof(string), modConfigType]);
            if (registerMethod == null)
            {
                MainFile.Logger.Warn("BaseLib ModConfigRegistry.Register method not found");
                return false;
            }

            registerMethod.Invoke(null, [ModId, adapter]);

            var eventInfo = adapter.GetType().GetEvent("ConfigChanged");
            if (eventInfo != null)
                eventInfo.AddEventHandler(adapter, new EventHandler(OnBaseLibConfigChanged));

            s_registered = true;
            MainFile.Logger.Info("Registered config via BaseLib (direct reflection)");
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to register config with BaseLib: {ex.Message}");
            return false;
        }
    }

    private static object? CreateDynamicAdapter()
    {
        try
        {
            if (s_dynamicAdapterType != null && s_dynamicAdapterInstance != null)
                return s_dynamicAdapterInstance;

            var simpleModConfigType = ResolveTypeAcrossAssemblies("BaseLib.Config.SimpleModConfig");
            var sectionAttrType = ResolveTypeAcrossAssemblies("BaseLib.Config.ConfigSectionAttribute");
            var hoverTipAttrType = ResolveTypeAcrossAssemblies("BaseLib.Config.ConfigHoverTipAttribute");
            var sliderAttrType = ResolveTypeAcrossAssemblies("BaseLib.Config.ConfigSliderAttribute");

            if (simpleModConfigType == null || sectionAttrType == null)
                return null;

            var sectionCtor = sectionAttrType.GetConstructor([typeof(string)]);
            var hoverTipCtor = hoverTipAttrType?.GetConstructor(Type.EmptyTypes)
                ?? hoverTipAttrType?.GetConstructor([typeof(bool)]);

            var asmName = new AssemblyName("YuWanCard.DynamicConfig");
            var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            var modBuilder = asmBuilder.DefineDynamicModule("MainModule");
            var typeBuilder = modBuilder.DefineType(
                "YuWanCard.Config.YuWanCardConfigAdapter",
                TypeAttributes.Public | TypeAttributes.Class,
                simpleModConfigType);

            var sliderCtor = sliderAttrType?.GetConstructor([typeof(double), typeof(double), typeof(double)]);
            var formatProp = sliderAttrType?.GetProperty("Format");

            // Emit in a unified Order sequence so BaseLib (which groups by section in emit order)
            // keeps same-section settings contiguous instead of creating duplicate section headers.
            var toggleByOrder = ToggleProps.Select(t => (t.Order, Emit: (Action)(() =>
                EmitBaseLibBoolProperty(typeBuilder, sectionCtor, hoverTipCtor, t.PropertyName, t.BaseLibSection))));
            var sliderByOrder = SliderProps.Select(s => (s.Order, Emit: (Action)(() =>
            {
                if (sliderCtor != null)
                    EmitBaseLibDoubleProperty(typeBuilder, sectionCtor, hoverTipCtor, sliderCtor, formatProp, s);
            })));

            foreach (var (_, emit) in toggleByOrder.Concat(sliderByOrder).OrderBy(x => x.Order))
                emit();

            s_dynamicAdapterType = typeBuilder.CreateType();
            if (s_dynamicAdapterType == null) return null;

            foreach (var t in ToggleProps)
                SetAdapterBool(t.PropertyName, GetConfigBool(t.PropertyName));
            foreach (var s in SliderProps)
                SetAdapterDouble(s.PropertyName, GetConfigDouble(s.PropertyName));

            s_dynamicAdapterInstance = Activator.CreateInstance(s_dynamicAdapterType);
            if (s_dynamicAdapterInstance == null) return null;

            foreach (var t in ToggleProps)
                SetConfigBool(t.PropertyName, GetAdapterBool(t.PropertyName));
            foreach (var s in SliderProps)
                SetConfigDouble(s.PropertyName, GetAdapterDouble(s.PropertyName));

            return s_dynamicAdapterInstance;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to create dynamic config adapter: {ex.Message}");
            return null;
        }
    }

    private static void EmitBaseLibBoolProperty(
        TypeBuilder typeBuilder, ConstructorInfo? sectionCtor, ConstructorInfo? hoverTipCtor,
        string name, string section)
    {
        var field = typeBuilder.DefineField(
            $"<{name}>k__BackingField", typeof(bool),
            FieldAttributes.Private | FieldAttributes.Static);

        var prop = typeBuilder.DefineProperty(name, PropertyAttributes.None, typeof(bool), null);

        if (sectionCtor != null)
            prop.SetCustomAttribute(new CustomAttributeBuilder(sectionCtor, [section]));
        ApplyHoverTip(prop, hoverTipCtor);

        EmitStaticAutoProperty(typeBuilder, prop, field, typeof(bool),
            nameof(RitsuConfigRuntimeBridge.ApplyRuntimeBool), name);
    }

    private static void EmitBaseLibDoubleProperty(
        TypeBuilder typeBuilder, ConstructorInfo? sectionCtor, ConstructorInfo? hoverTipCtor,
        ConstructorInfo? sliderCtor, PropertyInfo? formatProp,
        SliderSettingDefinition s)
    {
        var field = typeBuilder.DefineField(
            $"<{s.PropertyName}>k__BackingField", typeof(double),
            FieldAttributes.Private | FieldAttributes.Static);

        var prop = typeBuilder.DefineProperty(s.PropertyName, PropertyAttributes.None, typeof(double), null);

        if (sectionCtor != null)
            prop.SetCustomAttribute(new CustomAttributeBuilder(sectionCtor, [s.BaseLibSection]));
        ApplyHoverTip(prop, hoverTipCtor);

        if (sliderCtor != null)
        {
            if (formatProp != null)
                prop.SetCustomAttribute(new CustomAttributeBuilder(
                    sliderCtor, [s.Min, s.Max, s.Step], [formatProp], [s.Format]));
            else
                prop.SetCustomAttribute(new CustomAttributeBuilder(sliderCtor, [s.Min, s.Max, s.Step]));
        }

        EmitStaticAutoProperty(typeBuilder, prop, field, typeof(double),
            nameof(RitsuConfigRuntimeBridge.ApplyRuntimeDouble), s.PropertyName);
    }

    private static void ApplyHoverTip(PropertyBuilder prop, ConstructorInfo? hoverTipCtor)
    {
        if (hoverTipCtor == null) return;
        var args = hoverTipCtor.GetParameters().Length == 0 ? Array.Empty<object>() : [true];
        prop.SetCustomAttribute(new CustomAttributeBuilder(hoverTipCtor, args));
    }

    /// <summary>Emits a static get/set property backed by <paramref name="field"/>; the setter also
    /// calls the named RitsuConfigRuntimeBridge apply method to mirror the value into YuWanCardConfig.</summary>
    private static void EmitStaticAutoProperty(
        TypeBuilder typeBuilder, PropertyBuilder prop, FieldBuilder field, Type valueType,
        string bridgeMethodName, string propName)
    {
        var getter = typeBuilder.DefineMethod(
            $"get_{propName}",
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName,
            valueType, Type.EmptyTypes);
        var getIL = getter.GetILGenerator();
        getIL.Emit(OpCodes.Ldsfld, field);
        getIL.Emit(OpCodes.Ret);
        prop.SetGetMethod(getter);

        var setter = typeBuilder.DefineMethod(
            $"set_{propName}",
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName,
            null, [valueType]);
        var setIL = setter.GetILGenerator();
        setIL.Emit(OpCodes.Ldarg_0);
        setIL.Emit(OpCodes.Stsfld, field);
        setIL.Emit(OpCodes.Ldstr, propName);
        setIL.Emit(OpCodes.Ldarg_0);
        setIL.Emit(OpCodes.Call, typeof(RitsuConfigRuntimeBridge).GetMethod(
            bridgeMethodName, BindingFlags.Public | BindingFlags.Static)!);
        setIL.Emit(OpCodes.Ret);
        prop.SetSetMethod(setter);
    }

    private static void OnBaseLibConfigChanged(object? sender, EventArgs e)
    {
        if (s_dynamicAdapterType == null) return;
        foreach (var t in ToggleProps)
            SetConfigBool(t.PropertyName, GetAdapterBool(t.PropertyName));
        foreach (var s in SliderProps)
            SetConfigDouble(s.PropertyName, GetAdapterDouble(s.PropertyName));

        Patches.CursorReplacePatch.RefreshCursor();
    }

    private static void SetAdapterBool(string name, bool value)
    {
        try { s_dynamicAdapterType?.GetProperty(name)?.SetValue(null, value); }
        catch { }
    }

    private static bool GetAdapterBool(string name)
    {
        try { return (bool)s_dynamicAdapterType!.GetProperty(name)!.GetValue(null)!; }
        catch { return false; }
    }

    private static void SetAdapterDouble(string name, double value)
    {
        try { s_dynamicAdapterType?.GetProperty(name)?.SetValue(null, value); }
        catch { }
    }

    private static double GetAdapterDouble(string name)
    {
        try { return (double)s_dynamicAdapterType!.GetProperty(name)!.GetValue(null)!; }
        catch { return 0d; }
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
            var bindingAttrType = ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsBindingAttribute");
            var bindingSourceType = ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsReflectionBindingSource");

            if (pageAttrType == null || sectionAttrType == null || toggleAttrType == null)
                return false;

            var dynamicTypes = CreateRitsuConfigTypes(pageAttrType, sectionAttrType, toggleAttrType, subpageAttrType,
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
        Type? sliderAttrType, Type? bindingAttrType, Type? bindingSourceType)
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
            toggleCtor, [setting.RitsuId, setting.RitsuSectionId], labelProp, descProp, orderProp, setting.Label, setting.Description, setting.Order));

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
            sliderCtor, [setting.RitsuId, setting.RitsuSectionId, setting.Min, setting.Max, setting.Step], labelProp, descProp, orderProp, setting.Label, setting.Description, setting.Order));

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

        if (buttonTextProp != null && setting.ButtonText != null)
        {
            props.Add(buttonTextProp);
            values.Add(setting.ButtonText);
        }

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
        string label, string? description, int order)
    {
        var props = new List<PropertyInfo>();
        var values = new List<object>();

        if (labelProp != null) { props.Add(labelProp); values.Add(label); }
        if (descProp != null && description != null) { props.Add(descProp); values.Add(description); }
        if (orderProp != null) { props.Add(orderProp); values.Add(order); }

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
}
