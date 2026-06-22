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

    private static bool s_registered;
    private static bool s_ritsuRegistered;

    private static Type? s_dynamicAdapterType;
    private static object? s_dynamicAdapterInstance;

    // Boolean toggle settings. Section is the BaseLib section title; RitsuLib uses a fixed "display" section.
    // Order controls UI position (shared across both backends; lower = earlier).
    private static readonly (string PropertyName, string Section, string RitsuId, string DataKey, string Label, string? Description, int Order)[]
        ToggleProps =
    [
        ("EnableDeathEffect", "显示设置", "enable_death_effect", "config_enable_death_effect", "死亡特效", "击败敌人时显示死亡特效", 0),
        ("EnableCustomCursor", "显示设置", "enable_custom_cursor", "config_enable_custom_cursor", "自定义鼠标指针", "用猪猪主题指针替换游戏默认鼠标指针", 1),
        ("BypassModelDbHashCheck", "多人游戏设置", "bypass_modeldb_check", "config_bypass_modeldb_hash_check", "跳过哈希检查", "多人模式下跳过ModelDb哈希校验", 3),
        ("EnableAutoUpdateCheck", "更新设置", "enable_auto_update", "config_enable_auto_update_check", "自动检查更新", "启动时自动检查模组更新", 4),
        ("EnableSevenCursesRing", "游戏设置", "enable_seven_curses_ring", "config_enable_seven_curses_ring", "七咒之戒", "在Neow处可选择七咒之戒", 5),
        ("EnableMaliceSelection", "游戏设置", "enable_malice_selection", "config_enable_malice_selection", "恶意难度选择", "在角色选择界面显示恶意难度选择面板", 6),
    ];

    // Double slider settings: (Property, Section, RitsuId, DataKey, Label, Description, Min, Max, Step, Format, Order).
    private static readonly (string PropertyName, string Section, string RitsuId, string DataKey, string Label,
        string? Description, double Min, double Max, double Step, string Format, int Order)[] SliderProps =
    [
        ("CursorScale", "显示设置", "cursor_scale", "config_cursor_scale", "鼠标指针缩放",
            "自定义鼠标指针的大小，1.0x 约为原版的 64px", 0.1, 10.0, 0.1, "{0}x", 2),
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
                EmitBaseLibBoolProperty(typeBuilder, sectionCtor, hoverTipCtor, t.PropertyName, t.Section))));
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
        (string PropertyName, string Section, string RitsuId, string DataKey, string Label,
            string? Description, double Min, double Max, double Step, string Format, int Order) s)
    {
        var field = typeBuilder.DefineField(
            $"<{s.PropertyName}>k__BackingField", typeof(double),
            FieldAttributes.Private | FieldAttributes.Static);

        var prop = typeBuilder.DefineProperty(s.PropertyName, PropertyAttributes.None, typeof(double), null);

        if (sectionCtor != null)
            prop.SetCustomAttribute(new CustomAttributeBuilder(sectionCtor, [s.Section]));
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
            var bindingAttrType = ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsBindingAttribute");
            var bindingSourceType = ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsReflectionBindingSource");

            if (pageAttrType == null || sectionAttrType == null || toggleAttrType == null)
                return false;

            var dynamicType = CreateRitsuConfigType(pageAttrType, sectionAttrType, toggleAttrType,
                sliderAttrType, bindingAttrType, bindingSourceType);
            if (dynamicType == null) return false;

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

            var pagesRegistered = registerMethod.Invoke(null, [dynamicType]) as int?;
            MainFile.Logger.Info($"Registered {pagesRegistered ?? 0} config page(s) via STS2-RitsuLib (direct reflection)");

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

    private static Type? CreateRitsuConfigType(
        Type pageAttrType, Type sectionAttrType, Type toggleAttrType,
        Type? sliderAttrType, Type? bindingAttrType, Type? bindingSourceType)
    {
        try
        {
            var pageCtor = pageAttrType.GetConstructor([typeof(string), typeof(string)]);
            if (pageCtor == null) return null;

            var sectionCtor = sectionAttrType.GetConstructor([typeof(string)]);

            var toggleCtor = toggleAttrType.GetConstructor([typeof(string), typeof(string)]);
            if (toggleCtor == null) return null;

            var labelProp = toggleAttrType.GetProperty("Label");
            var descProp = toggleAttrType.GetProperty("Description");
            var orderProp = toggleAttrType.GetProperty("Order");
            var titleProp = pageAttrType.GetProperty("Title");
            var modDisplayProp = pageAttrType.GetProperty("ModDisplayName");

            var asmName = new AssemblyName("YuWanCard.DynamicRitsuConfig");
            var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            var modBuilder = asmBuilder.DefineDynamicModule("RitsuModule");
            var typeBuilder = modBuilder.DefineType(
                "YuWanCard.Config.YuWanCardRitsuConfigProvider",
                TypeAttributes.Public | TypeAttributes.Class);

            if (titleProp != null && modDisplayProp != null)
                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                    pageCtor, [ModId, "yuwan_card"],
                    [titleProp, modDisplayProp],
                    ["YuWanCard 设置", "YuWanCard"]));
            else
                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(pageCtor, [ModId, "yuwan_card"]));

            var sectionTitleProp = sectionAttrType.GetProperty("Title");
            if (sectionCtor != null)
            {
                if (sectionTitleProp != null)
                    typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                        sectionCtor, ["display"],
                        [sectionTitleProp], ["显示"]));
                else
                    typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(sectionCtor, ["display"]));
            }

            foreach (var t in ToggleProps)
                EmitRitsuBoolProperty(typeBuilder, toggleCtor, labelProp, descProp, orderProp,
                    bindingAttrType, bindingSourceType, t.PropertyName, t.RitsuId, t.DataKey, t.Label, t.Description, t.Order);

            if (sliderAttrType != null)
            {
                var sliderCtor = sliderAttrType.GetConstructor(
                    [typeof(string), typeof(string), typeof(double), typeof(double), typeof(double)]);
                var sliderLabelProp = sliderAttrType.GetProperty("Label");
                var sliderDescProp = sliderAttrType.GetProperty("Description");
                var sliderOrderProp = sliderAttrType.GetProperty("Order");

                if (sliderCtor != null)
                    foreach (var s in SliderProps)
                        EmitRitsuDoubleProperty(typeBuilder, sliderCtor, sliderLabelProp, sliderDescProp, sliderOrderProp,
                            bindingAttrType, bindingSourceType, s.PropertyName, s.RitsuId, s.DataKey,
                            s.Label, s.Description, s.Min, s.Max, s.Step, s.Order);
            }

            return typeBuilder.CreateType();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to create dynamic Ritsu config type: {ex.Message}");
            return null;
        }
    }

    private static void EmitRitsuBoolProperty(
        TypeBuilder typeBuilder, ConstructorInfo toggleCtor,
        PropertyInfo? labelProp, PropertyInfo? descProp, PropertyInfo? orderProp,
        Type? bindingAttrType, Type? bindingSourceType,
        string propName, string toggleId, string dataKey, string label, string? description, int order)
    {
        var prop = typeBuilder.DefineProperty(propName, PropertyAttributes.None, typeof(bool), null);

        prop.SetCustomAttribute(BuildEntryAttribute(
            toggleCtor, [toggleId, "display"], labelProp, descProp, orderProp, label, description, order));

        if (TryCreateRitsuBindingAttribute(bindingAttrType, bindingSourceType, dataKey) is { } bindingAttrBuilder)
            prop.SetCustomAttribute(bindingAttrBuilder);

        var getter = typeBuilder.DefineMethod(
            $"get_{propName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            typeof(bool), Type.EmptyTypes);
        var getIL = getter.GetILGenerator();
        getIL.Emit(OpCodes.Ldstr, propName);
        getIL.Emit(OpCodes.Call, typeof(RitsuConfigRuntimeBridge).GetMethod(
            nameof(RitsuConfigRuntimeBridge.ReadRuntimeBool),
            BindingFlags.Public | BindingFlags.Static)!);
        getIL.Emit(OpCodes.Ret);
        prop.SetGetMethod(getter);

        var setter = typeBuilder.DefineMethod(
            $"set_{propName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            null, [typeof(bool)]);
        var setIL = setter.GetILGenerator();
        setIL.Emit(OpCodes.Ldstr, propName);
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
        string propName, string entryId, string dataKey, string label, string? description,
        double min, double max, double step, int order)
    {
        var prop = typeBuilder.DefineProperty(propName, PropertyAttributes.None, typeof(double), null);

        prop.SetCustomAttribute(BuildEntryAttribute(
            sliderCtor, [entryId, "display", min, max, step], labelProp, descProp, orderProp, label, description, order));

        if (TryCreateRitsuBindingAttribute(bindingAttrType, bindingSourceType, dataKey) is { } bindingAttrBuilder)
            prop.SetCustomAttribute(bindingAttrBuilder);

        var getter = typeBuilder.DefineMethod(
            $"get_{propName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            typeof(double), Type.EmptyTypes);
        var getIL = getter.GetILGenerator();
        getIL.Emit(OpCodes.Ldstr, propName);
        getIL.Emit(OpCodes.Call, typeof(RitsuConfigRuntimeBridge).GetMethod(
            nameof(RitsuConfigRuntimeBridge.ReadRuntimeDouble),
            BindingFlags.Public | BindingFlags.Static)!);
        getIL.Emit(OpCodes.Ret);
        prop.SetGetMethod(getter);

        var setter = typeBuilder.DefineMethod(
            $"set_{propName}",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            null, [typeof(double)]);
        var setIL = setter.GetILGenerator();
        setIL.Emit(OpCodes.Ldstr, propName);
        setIL.Emit(OpCodes.Ldarg_1);
        setIL.Emit(OpCodes.Call, typeof(RitsuConfigRuntimeBridge).GetMethod(
            nameof(RitsuConfigRuntimeBridge.ApplyRuntimeDouble),
            BindingFlags.Public | BindingFlags.Static)!);
        setIL.Emit(OpCodes.Ret);
        prop.SetSetMethod(setter);
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
