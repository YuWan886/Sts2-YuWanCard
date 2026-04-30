using System.Reflection;
using System.Reflection.Emit;

namespace YuWanCard;

internal static class ConfigRegistrar
{
    private const string ModId = MainFile.ModId;

    private static bool s_registered;
    private static bool s_ritsuRegistered;

    private static Type? s_dynamicAdapterType;
    private static object? s_dynamicAdapterInstance;

    private static readonly string[] ConfigKeys =
    [
        "EnableDeathEffect",
        "BypassModelDbHashCheck",
        "EnableAutoUpdateCheck",
        "EnableAutoSlay",
        "EnableSevenCursesRing",
    ];

    private static readonly (string Name, string Section)[] BaseLibConfigProps =
    [
        ("EnableDeathEffect", "显示设置"),
        ("BypassModelDbHashCheck", "多人游戏设置"),
        ("EnableAutoUpdateCheck", "更新设置"),
        ("EnableAutoSlay", "自动爬塔设置"),
        ("EnableSevenCursesRing", "游戏设置"),
    ];

    private static readonly (string PropertyName, string ToggleId, string Label, string? Description)[] RitsuConfigProps =
    [
        ("EnableDeathEffect", "enable_death_effect", "死亡特效", "击败敌人时显示死亡特效"),
        ("BypassModelDbHashCheck", "bypass_modeldb_check", "跳过哈希检查", "多人模式下跳过ModelDb哈希校验"),
        ("EnableAutoUpdateCheck", "enable_auto_update", "自动检查更新", "启动时自动检查模组更新"),
        ("EnableAutoSlay", "enable_auto_slay", "自动爬塔", "自动进行角色选择并开始爬塔"),
        ("EnableSevenCursesRing", "enable_seven_curses_ring", "七咒之戒", "在Neow处可选择七咒之戒"),
    ];

    public static void TryDeferredRegister()
    {
        if (s_registered || MainFile.Config == null) return;

        if (IsBaseLibAvailable() && TryRegisterBaseLib())
            return;

        if (IsRitsuLibAvailable())
            TryRegisterRitsuLib();
    }

    private static bool IsBaseLibAvailable()
    {
        return ResolveTypeAcrossAssemblies("BaseLib.Config.SimpleModConfig") != null
            || Type.GetType("BaseLib.Config.SimpleModConfig, BaseLib") != null;
    }

    private static bool IsRitsuLibAvailable()
    {
        return ResolveTypeAcrossAssemblies("STS2RitsuLib.RitsuLibFramework") != null;
    }

    private static bool TryRegisterBaseLib()
    {
        try
        {
            var adapter = CreateDynamicAdapter();
            if (adapter == null) return false;

            var registryType = Type.GetType("BaseLib.Config.ModConfigRegistry, BaseLib");
            registryType?.GetMethod("Register")?.Invoke(null, [ModId, adapter]);

            var eventInfo = adapter.GetType().GetEvent("ConfigChanged");
            if (eventInfo != null)
            {
                eventInfo.AddEventHandler(adapter, new EventHandler(OnConfigChanged));
            }

            s_registered = true;
            MainFile.Logger.Info("Registered config via BaseLib (dynamic adapter)");
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to register config with BaseLib: {ex.Message}");
            return false;
        }
    }

    private static bool TryRegisterRitsuLib()
    {
        if (s_ritsuRegistered) return true;

        try
        {
            var ritsuFrameworkType = ResolveTypeAcrossAssemblies("STS2RitsuLib.RitsuLibFramework");
            if (ritsuFrameworkType == null) return false;

            var pageAttrType = ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsPageAttribute");
            var sectionAttrType = ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsSectionAttribute");
            var toggleAttrType = ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsToggleAttribute");
            var bindingAttrType = ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsBindingAttribute");
            var bindingSourceType = ResolveTypeAcrossAssemblies("STS2RitsuLib.Settings.ModSettingsReflectionBindingSource");

            if (pageAttrType == null || sectionAttrType == null || toggleAttrType == null)
            {
                MainFile.Logger.Debug("STS2-RitsuLib detected but config attribute types not found");
                return false;
            }

            var pageCtor = pageAttrType.GetConstructor([typeof(string), typeof(string)]);
            if (pageCtor == null) return false;

            var sectionCtor = sectionAttrType.GetConstructor([typeof(string)]);
            if (sectionCtor == null) return false;

            var toggleCtor = toggleAttrType.GetConstructor([typeof(string), typeof(string)]);
            if (toggleCtor == null) return false;

            var labelProp = toggleAttrType.GetProperty("Label");
            var descProp = toggleAttrType.GetProperty("Description");
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
            if (sectionTitleProp != null)
                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                    sectionCtor, ["display"],
                    [sectionTitleProp], ["显示"]));
            else
                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(sectionCtor, ["display"]));

            CustomAttributeBuilder? bindingAttrBuilder = null;
            if (bindingAttrType != null && bindingSourceType != null)
            {
                var bindingCtor = bindingAttrType.GetConstructor(Type.EmptyTypes);
                if (bindingCtor != null)
                {
                    var globalVal = Enum.Parse(bindingSourceType, "Global");
                    bindingAttrBuilder = new CustomAttributeBuilder(
                        bindingCtor,
                        Array.Empty<object>(),
                        [bindingAttrType.GetProperty("Source")!],
                        [globalVal]);
                }
            }

            foreach (var (propName, toggleId, label, description) in RitsuConfigProps)
            {
                var field = typeBuilder.DefineField(
                    $"<{propName}>k__BackingField",
                    typeof(bool),
                    FieldAttributes.Private | FieldAttributes.Static);

                var prop = typeBuilder.DefineProperty(
                    propName,
                    PropertyAttributes.None,
                    typeof(bool),
                    null);

                if (labelProp != null && description != null && descProp != null)
                    prop.SetCustomAttribute(new CustomAttributeBuilder(
                        toggleCtor, [toggleId, "display"],
                        [labelProp, descProp],
                        [label, description]));
                else if (labelProp != null)
                    prop.SetCustomAttribute(new CustomAttributeBuilder(
                        toggleCtor, [toggleId, "display"],
                        [labelProp], [label]));
                else
                    prop.SetCustomAttribute(new CustomAttributeBuilder(toggleCtor, [toggleId, "display"]));

                if (bindingAttrBuilder != null)
                    prop.SetCustomAttribute(bindingAttrBuilder);

                var getter = typeBuilder.DefineMethod(
                    $"get_{propName}",
                    MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName,
                    typeof(bool),
                    Type.EmptyTypes);
                var getIL = getter.GetILGenerator();
                getIL.Emit(OpCodes.Ldsfld, field);
                getIL.Emit(OpCodes.Ret);
                prop.SetGetMethod(getter);

                var setter = typeBuilder.DefineMethod(
                    $"set_{propName}",
                    MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName,
                    null,
                    [typeof(bool)]);
                var setIL = setter.GetILGenerator();
                setIL.Emit(OpCodes.Ldarg_0);
                setIL.Emit(OpCodes.Stsfld, field);
                setIL.Emit(OpCodes.Ret);
                prop.SetSetMethod(setter);
            }

            var dynamicType = typeBuilder.CreateType();
            if (dynamicType == null) return false;

            if (MainFile.Config != null)
            {
                foreach (var key in ConfigKeys)
                    SetDynamicProperty(dynamicType, key, GetConfigValue(key));
            }

            var registerMethod = ritsuFrameworkType.GetMethod(
                "RegisterModSettingsReflectionProviderAndTryRegister",
                [typeof(Type)]);
            if (registerMethod == null) return false;

            var pagesRegistered = (int?)registerMethod.Invoke(null, [dynamicType]);
            MainFile.Logger.Info($"Registered {pagesRegistered ?? 0} config page(s) via STS2-RitsuLib (dynamic reflection provider)");

            SyncRitsuLibToConfig(ritsuFrameworkType);

            s_ritsuRegistered = true;
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to register config with STS2-RitsuLib: {ex.Message}");
            return false;
        }
    }

    private static object? CreateDynamicAdapter()
    {
        try
        {
            if (s_dynamicAdapterType != null && s_dynamicAdapterInstance != null)
                return s_dynamicAdapterInstance;

            var simpleModConfigType = Type.GetType("BaseLib.Config.SimpleModConfig, BaseLib");
            var sectionAttrType = Type.GetType("BaseLib.Config.ConfigSectionAttribute, BaseLib");
            var hoverTipAttrType = Type.GetType("BaseLib.Config.ConfigHoverTipAttribute, BaseLib");

            if (simpleModConfigType == null || sectionAttrType == null)
                return null;

            var sectionCtor = sectionAttrType.GetConstructor([typeof(string)]);
            var hoverTipCtor = hoverTipAttrType?
                .GetConstructor(Type.EmptyTypes)
                ?? hoverTipAttrType?.GetConstructor([typeof(bool)]);

            var asmName = new AssemblyName("YuWanCard.DynamicConfig");
            var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            var modBuilder = asmBuilder.DefineDynamicModule("MainModule");
            var typeBuilder = modBuilder.DefineType(
                "YuWanCard.Config.YuWanCardConfigAdapter",
                TypeAttributes.Public | TypeAttributes.Class,
                simpleModConfigType);

            foreach (var (name, section) in BaseLibConfigProps)
            {
                var field = typeBuilder.DefineField(
                    $"<{name}>k__BackingField",
                    typeof(bool),
                    FieldAttributes.Private | FieldAttributes.Static);

                var prop = typeBuilder.DefineProperty(
                    name,
                    PropertyAttributes.None,
                    typeof(bool),
                    null);

                if (sectionCtor != null)
                    prop.SetCustomAttribute(new CustomAttributeBuilder(sectionCtor, [section]));

                if (hoverTipCtor != null)
                {
                    var hoverArgs = hoverTipCtor.GetParameters().Length == 0
                        ? Array.Empty<object>()
                        : [true];
                    prop.SetCustomAttribute(new CustomAttributeBuilder(hoverTipCtor, hoverArgs));
                }

                var getter = typeBuilder.DefineMethod(
                    $"get_{name}",
                    MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName,
                    typeof(bool),
                    Type.EmptyTypes);
                var getIL = getter.GetILGenerator();
                getIL.Emit(OpCodes.Ldsfld, field);
                getIL.Emit(OpCodes.Ret);
                prop.SetGetMethod(getter);

                var setter = typeBuilder.DefineMethod(
                    $"set_{name}",
                    MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName,
                    null,
                    [typeof(bool)]);
                var setIL = setter.GetILGenerator();
                setIL.Emit(OpCodes.Ldarg_0);
                setIL.Emit(OpCodes.Stsfld, field);
                setIL.Emit(OpCodes.Ret);
                prop.SetSetMethod(setter);
            }

            s_dynamicAdapterType = typeBuilder.CreateType();
            if (s_dynamicAdapterType == null) return null;

            foreach (var key in ConfigKeys)
                SetAdapterProperty(key, GetConfigValue(key));

            s_dynamicAdapterInstance = Activator.CreateInstance(s_dynamicAdapterType);
            if (s_dynamicAdapterInstance == null) return null;

            foreach (var key in ConfigKeys)
                SetConfigValue(key, GetAdapterBool(key));

            return s_dynamicAdapterInstance;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to create dynamic config adapter: {ex.Message}");
            return null;
        }
    }

    private static void SyncRitsuLibToConfig(Type ritsuFrameworkType)
    {
        try
        {
            var getDataStoreMethod = ritsuFrameworkType.GetMethod("GetDataStore");
            var dataStore = getDataStoreMethod?.Invoke(null, [ModId]);
            if (dataStore == null) return;

            var initGlobalMethod = dataStore.GetType().GetMethod("InitializeGlobal");
            initGlobalMethod?.Invoke(dataStore, null);

            var ritsuAsm = ritsuFrameworkType.Assembly;
            var mirrorSourceType = ritsuAsm.GetType("STS2RitsuLib.Settings.RuntimeReflectionMirrorSource");
            var boxOpenType = mirrorSourceType?.GetNestedType("ReflectionBindingBox`1",
                BindingFlags.NonPublic);
            var boxBoolType = boxOpenType?.MakeGenericType(typeof(bool));
            if (boxBoolType == null) return;

            var getMethod = dataStore.GetType().GetMethod("Get", [typeof(string)]);
            var getTypedMethod = getMethod?.MakeGenericMethod(boxBoolType);
            if (getTypedMethod == null) return;

            var valueProp = boxBoolType.GetProperty("Value");
            if (valueProp == null) return;

            foreach (var propName in ConfigKeys)
            {
                var dataKey = $"reflect::YuWanCard.Config.YuWanCardRitsuConfigProvider.{propName}";
                try
                {
                    var box = getTypedMethod.Invoke(dataStore, [dataKey]);
                    if (box != null)
                    {
                        var savedValue = (bool)valueProp.GetValue(box)!;
                        SetConfigValue(propName, savedValue);
                    }
                }
                catch
                {
                    // Key may not exist yet (first run); default is correct
                }
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to sync RitsuLib config to YuWanCardConfig: {ex.Message}");
        }
    }

    private static void OnConfigChanged(object? sender, EventArgs e)
    {
        if (s_dynamicAdapterType == null) return;
        foreach (var key in ConfigKeys)
            SetConfigValue(key, GetAdapterBool(key));
    }

    private static void SetAdapterProperty(string name, bool value)
    {
        try { s_dynamicAdapterType?.GetProperty(name)?.SetValue(null, value); }
        catch { /* best-effort */ }
    }

    private static bool GetAdapterBool(string name)
    {
        try { return (bool)s_dynamicAdapterType!.GetProperty(name)!.GetValue(null)!; }
        catch { return false; }
    }

    private static void SetDynamicProperty(Type dynamicType, string name, bool value)
    {
        try { dynamicType.GetProperty(name)?.SetValue(null, value); }
        catch { /* best-effort */ }
    }

    private static bool GetConfigValue(string name)
    {
        return (bool)typeof(Config.YuWanCardConfig).GetProperty(name)!.GetValue(null)!;
    }

    private static void SetConfigValue(string name, bool value)
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
            catch
            {
                // Some assemblies (dynamic emit, reflection-only) may throw
            }
        }
        return null;
    }
}
