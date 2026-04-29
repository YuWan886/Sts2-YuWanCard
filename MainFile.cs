using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YuWanCard.Config;
using YuWanCard.Multiplayer;
using YuWanCard.Patches;
using YuWanCard.Utils;

namespace YuWanCard;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YuWanCard";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static YuWanCardConfig? Config { get; private set; }

    private static bool s_configRegistered;
    private static bool s_ritsuConfigRegistered;

    private static Type? s_dynamicAdapterType;
    private static object? s_dynamicAdapterInstance;

    private const string PigVisualsPath = "res://YuWanCard/scenes/characters/pig.tscn";
    private const string PigMerchantPath = "res://YuWanCard/scenes/characters/pig_merchant.tscn";
    private const string PigEnergyCounterPath = "res://YuWanCard/scenes/characters/pig_energy_counter.tscn";
    private const string PigRestSitePath = "res://YuWanCard/scenes/rest_site/characters/pig_rest_site.tscn";

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        EndlessModePatch.ApplyMapPointTypeCountsPatches(harmony);
        AutoSlayCharacterPatch.ApplyPatch(harmony);
        AutoSlayOptionsPatch.ApplyPatch(harmony);

        ContentRegistry.RegisterAll(Assembly.GetExecutingAssembly());

        Config = new YuWanCardConfig();
        RegisterConfig();

        NodeFactory.Init();
        RegisterSceneConversions();

        TeammatePayMessageHandler.Register();

        PreloadAssets();

        Logger.Info("YuWanCard initialized");
    }

    private static void RegisterConfig()
    {

    }

    public static void TryDeferredConfigRegister()
    {
        if (s_configRegistered || Config == null) return;

        bool baseLibAvailable = IsBaseLibAvailable();
        bool ritsuLibAvailable = IsRitsuLibAvailable();

        if (baseLibAvailable)
        {
            if (TryRegisterBaseLibConfig())
                return;
        }

        if (ritsuLibAvailable)
        {
            if (TryRegisterRitsuLibConfig())
                return;
        }
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

    private static bool TryRegisterBaseLibConfig()
    {
        try
        {
            var adapter = CreateDynamicConfigAdapter();
            if (adapter == null) return false;

            var registryType = Type.GetType("BaseLib.Config.ModConfigRegistry, BaseLib");
            var registerMethod = registryType?.GetMethod("Register");
            registerMethod?.Invoke(null, [ModId, adapter]);

            var eventInfo = adapter.GetType().GetEvent("ConfigChanged");
            if (eventInfo != null)
            {
                var handler = new EventHandler(OnConfigChanged);
                eventInfo.AddEventHandler(adapter, handler);
            }

            s_configRegistered = true;
            Logger.Info("Registered config via BaseLib (dynamic adapter)");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to register config with BaseLib: {ex.Message}");
            return false;
        }
    }

    private static bool TryRegisterRitsuLibConfig()
    {
        if (s_ritsuConfigRegistered) return true;

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
                Logger.Debug("STS2-RitsuLib detected but config attribute types not found");
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

            // [ModSettingsPage("YuWanCard", "yuwan_card", Title = "YuWanCard", ModDisplayName = "YuWanCard")]
            if (titleProp != null && modDisplayProp != null)
                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                    pageCtor, [ModId, "yuwan_card"],
                    [titleProp, modDisplayProp],
                    ["YuWanCard 设置", "YuWanCard"]));
            else
                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(pageCtor, [ModId, "yuwan_card"]));

            // [ModSettingsSection("display", Title = "Display")]
            var sectionTitleProp = sectionAttrType.GetProperty("Title");
            if (sectionTitleProp != null)
                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(
                    sectionCtor, ["display"],
                    [sectionTitleProp], ["显示"]));
            else
                typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(sectionCtor, ["display"]));

            // Properties with [ModSettingsToggle] + labels
            var props = new (string PropertyName, string ToggleId, string Label, string? Description)[] {
                ("EnableDeathEffect", "enable_death_effect", "死亡特效", "击败敌人时显示死亡特效"),
                ("BypassModelDbHashCheck", "bypass_modeldb_check", "跳过哈希检查", "多人模式下跳过ModelDb哈希校验"),
                ("EnableAutoUpdateCheck", "enable_auto_update", "自动检查更新", "启动时自动检查模组更新"),
                ("EnableAutoSlay", "enable_auto_slay", "自动爬塔", "自动进行角色选择并开始爬塔"),
            };

            // Build optional [ModSettingsBinding(Source = Global)] if types available
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

            foreach (var (propName, toggleId, label, description) in props)
            {
                // Static backing field
                var field = typeBuilder.DefineField(
                    $"<{propName}>k__BackingField",
                    typeof(bool),
                    FieldAttributes.Private | FieldAttributes.Static);

                // Property
                var prop = typeBuilder.DefineProperty(
                    propName,
                    PropertyAttributes.None,
                    typeof(bool),
                    null);

                // [ModSettingsToggle("toggleId", "display", Label = "...", Description = "...")]
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

                // [ModSettingsBinding(Source = Global)]
                if (bindingAttrBuilder != null)
                    prop.SetCustomAttribute(bindingAttrBuilder);

                // Static getter
                var getter = typeBuilder.DefineMethod(
                    $"get_{propName}",
                    MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName,
                    typeof(bool),
                    Type.EmptyTypes);
                var getIL = getter.GetILGenerator();
                getIL.Emit(OpCodes.Ldsfld, field);
                getIL.Emit(OpCodes.Ret);
                prop.SetGetMethod(getter);

                // Static setter
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

            // Create the type
            var dynamicType = typeBuilder.CreateType();
            if (dynamicType == null) return false;

            if (Config != null)
            {
                SetDynamicAdapterProperty(dynamicType, "EnableDeathEffect", YuWanCardConfig.EnableDeathEffect);
                SetDynamicAdapterProperty(dynamicType, "BypassModelDbHashCheck", YuWanCardConfig.BypassModelDbHashCheck);
                SetDynamicAdapterProperty(dynamicType, "EnableAutoUpdateCheck", YuWanCardConfig.EnableAutoUpdateCheck);
                SetDynamicAdapterProperty(dynamicType, "EnableAutoSlay", YuWanCardConfig.EnableAutoSlay);
            }

            var registerMethod = ritsuFrameworkType.GetMethod(
                "RegisterModSettingsReflectionProviderAndTryRegister",
                [typeof(Type)]);
            if (registerMethod == null) return false;

            var pagesRegistered = (int?)registerMethod.Invoke(null, [dynamicType]);
            Logger.Info($"Registered {pagesRegistered ?? 0} config page(s) via STS2-RitsuLib (dynamic reflection provider)");

            SyncRitsuLibConfigToYuWanCardConfig(ritsuFrameworkType);

            s_ritsuConfigRegistered = true;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to register config with STS2-RitsuLib: {ex.Message}");
            return false;
        }
    }

    private static object? CreateDynamicConfigAdapter()
    {
        try
        {
            // Reuse cached type if already created
            if (s_dynamicAdapterType != null && s_dynamicAdapterInstance != null)
                return s_dynamicAdapterInstance;

            var simpleModConfigType = Type.GetType("BaseLib.Config.SimpleModConfig, BaseLib");
            var sectionAttrType = Type.GetType("BaseLib.Config.ConfigSectionAttribute, BaseLib");
            var hoverTipAttrType = Type.GetType("BaseLib.Config.ConfigHoverTipAttribute, BaseLib");

            if (simpleModConfigType == null || sectionAttrType == null)
                return null;

            // Build attribute constructors
            var sectionCtor = sectionAttrType.GetConstructor([typeof(string)]);
            var hoverTipCtor = hoverTipAttrType?
                .GetConstructor(Type.EmptyTypes)
                ?? hoverTipAttrType?.GetConstructor([typeof(bool)]);

            // Define dynamic assembly and type
            var asmName = new AssemblyName("YuWanCard.DynamicConfig");
            var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            var modBuilder = asmBuilder.DefineDynamicModule("MainModule");
            var typeBuilder = modBuilder.DefineType(
                "YuWanCard.Config.YuWanCardConfigAdapter",
                TypeAttributes.Public | TypeAttributes.Class,
                simpleModConfigType);

            // Property definitions (name, section label)
            var props = new (string Name, string Section)[] {
                ("EnableDeathEffect", "显示设置"),
                ("BypassModelDbHashCheck", "多人游戏设置"),
                ("EnableAutoUpdateCheck", "更新设置"),
                ("EnableAutoSlay", "自动爬塔设置"),
            };

            var propBuilders = new List<(PropertyBuilder Prop, FieldBuilder Field)>();

            foreach (var (name, section) in props)
            {
                // Static backing field
                var field = typeBuilder.DefineField(
                    $"<{name}>k__BackingField",
                    typeof(bool),
                    FieldAttributes.Private | FieldAttributes.Static);

                // Property
                var prop = typeBuilder.DefineProperty(
                    name,
                    PropertyAttributes.None,
                    typeof(bool),
                    null);

                // [ConfigSection("...")]
                if (sectionCtor != null)
                    prop.SetCustomAttribute(new CustomAttributeBuilder(sectionCtor, [section]));

                // [ConfigHoverTip]
                if (hoverTipCtor != null)
                {
                    var hoverArgs = hoverTipCtor.GetParameters().Length == 0
                        ? Array.Empty<object>()
                        : [true];
                    prop.SetCustomAttribute(new CustomAttributeBuilder(hoverTipCtor, hoverArgs));
                }

                // Static getter: get_{Name}() => _field
                var getter = typeBuilder.DefineMethod(
                    $"get_{name}",
                    MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName,
                    typeof(bool),
                    Type.EmptyTypes);
                var getIL = getter.GetILGenerator();
                getIL.Emit(OpCodes.Ldsfld, field);
                getIL.Emit(OpCodes.Ret);
                prop.SetGetMethod(getter);

                // Static setter: set_{Name}(bool value) => _field = value
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

                propBuilders.Add((prop, field));
            }

            // Create the type
            s_dynamicAdapterType = typeBuilder.CreateType();
            if (s_dynamicAdapterType == null) return null;

            SetAdapterProperty("EnableDeathEffect", YuWanCardConfig.EnableDeathEffect);
            SetAdapterProperty("BypassModelDbHashCheck", YuWanCardConfig.BypassModelDbHashCheck);
            SetAdapterProperty("EnableAutoUpdateCheck", YuWanCardConfig.EnableAutoUpdateCheck);
            SetAdapterProperty("EnableAutoSlay", YuWanCardConfig.EnableAutoSlay);

            s_dynamicAdapterInstance = Activator.CreateInstance(s_dynamicAdapterType);
            if (s_dynamicAdapterInstance == null) return null;

            YuWanCardConfig.EnableDeathEffect = GetAdapterBool("EnableDeathEffect");
            YuWanCardConfig.BypassModelDbHashCheck = GetAdapterBool("BypassModelDbHashCheck");
            YuWanCardConfig.EnableAutoUpdateCheck = GetAdapterBool("EnableAutoUpdateCheck");
            YuWanCardConfig.EnableAutoSlay = GetAdapterBool("EnableAutoSlay");

            return s_dynamicAdapterInstance;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to create dynamic config adapter: {ex.Message}");
            return null;
        }
    }

    private static void SetAdapterProperty(string name, bool value)
    {
        try
        {
            s_dynamicAdapterType?.GetProperty(name)?.SetValue(null, value);
        }
        catch { /* best-effort sync */ }
    }

    private static bool GetAdapterBool(string name)
    {
        try
        {
            return (bool)s_dynamicAdapterType!.GetProperty(name)!.GetValue(null)!;
        }
        catch { return false; }
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
                // Some assemblies (like dynamic emit or reflection-only) may throw
            }
        }

        return null;
    }

    private static void SetDynamicAdapterProperty(Type dynamicType, string name, bool value)
    {
        try
        {
            dynamicType.GetProperty(name)?.SetValue(null, value);
        }
        catch { /* best-effort sync */ }
    }

    private static void SyncRitsuLibConfigToYuWanCardConfig(Type ritsuFrameworkType)
    {
        try
        {
            var getDataStoreMethod = ritsuFrameworkType.GetMethod("GetDataStore");
            var dataStore = getDataStoreMethod?.Invoke(null, [ModId]);
            if (dataStore == null) return;

            // Force-load all global-scoped entries from disk
            var initGlobalMethod = dataStore.GetType().GetMethod("InitializeGlobal");
            initGlobalMethod?.Invoke(dataStore, null);

            // Resolve the private ReflectionBindingBox<bool> type used as the store model
            var ritsuAsm = ritsuFrameworkType.Assembly;
            var mirrorSourceType = ritsuAsm.GetType("STS2RitsuLib.Settings.RuntimeReflectionMirrorSource");
            var boxOpenType = mirrorSourceType?.GetNestedType("ReflectionBindingBox`1",
                System.Reflection.BindingFlags.NonPublic);
            var boxBoolType = boxOpenType?.MakeGenericType(typeof(bool));
            if (boxBoolType == null) return;

            // Build the generic Get<T> method: store.Get<ReflectionBindingBox<bool>>(key)
            var getMethod = dataStore.GetType().GetMethod("Get", [typeof(string)]);
            var getTypedMethod = getMethod?.MakeGenericMethod(boxBoolType);
            if (getTypedMethod == null) return;

            var valueProp = boxBoolType.GetProperty("Value");
            if (valueProp == null) return;

            var keys = new[] {
                "EnableDeathEffect",
                "BypassModelDbHashCheck",
                "EnableAutoUpdateCheck",
                "EnableAutoSlay",
            };

            foreach (var propName in keys)
            {
                var dataKey = $"reflect::YuWanCard.Config.YuWanCardRitsuConfigProvider.{propName}";
                try
                {
                    var box = getTypedMethod.Invoke(dataStore, [dataKey]);
                    if (box != null)
                    {
                        var savedValue = (bool)valueProp.GetValue(box)!;
                        typeof(YuWanCardConfig).GetProperty(propName)?.SetValue(null, savedValue);
                    }
                }
                catch
                {
                    // Key may not exist yet (first run); the default set before registration is correct
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to sync RitsuLib config to YuWanCardConfig: {ex.Message}");
        }
    }

    private static void OnConfigChanged(object? sender, EventArgs e)
    {
        if (s_dynamicAdapterType == null) return;
        YuWanCardConfig.EnableDeathEffect = GetAdapterBool("EnableDeathEffect");
        YuWanCardConfig.BypassModelDbHashCheck = GetAdapterBool("BypassModelDbHashCheck");
        YuWanCardConfig.EnableAutoUpdateCheck = GetAdapterBool("EnableAutoUpdateCheck");
        YuWanCardConfig.EnableAutoSlay = GetAdapterBool("EnableAutoSlay");
    }

    private static void PreloadAssets()
    {
        VfxUtils.PreloadScenes(
            "res://YuWanCard/scenes/vfx/vfx_blood_wheel_eye.tscn",
            "res://YuWanCard/scenes/vfx/vfx_black_hole.tscn",
            "res://YuWanCard/scenes/vfx/vfx_glitch.tscn",
            "res://YuWanCard/scenes/vfx/vfx_glass_shatter.tscn",
            "res://YuWanCard/scenes/vfx/vfx_matrix_rain.tscn"
        );

        VfxUtils.PreloadFrames("res://YuWanCard/images/vfx/blood_wheel_eye/blood_wheel_eye", 48);

        PreloadTextures(
            "res://YuWanCard/images/characters/character_icon_pig.png",
            "res://YuWanCard/images/powers/pig_doubt_power.png"
        );
    }

    private static void RegisterSceneConversions()
    {
        NodeFactory.RegisterSceneType<NCreatureVisuals>(PigVisualsPath);
        NodeFactory.RegisterSceneType<NMerchantCharacter>(PigMerchantPath);
        NodeFactory.RegisterSceneType<NEnergyCounter>(PigEnergyCounterPath);
        NodeFactory.RegisterSceneType<NRestSiteCharacter>(PigRestSitePath);
    }

    private static void PreloadTextures(params string[] texturePaths)
    {
        int loadedCount = 0;
        foreach (var path in texturePaths)
        {
            if (ResourceLoader.Exists(path))
            {
                ResourceLoader.Load<Texture2D>(path);
                loadedCount++;
            }
            else
            {
                Logger.Warn($"PreloadTextures: Texture not found: {path}");
            }
        }
        if (loadedCount > 0)
        {
            Logger.Debug($"PreloadTextures: Preloaded {loadedCount} textures");
        }
    }
}

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
public static class NMainMenu_ConfigRegisterPatch
{
    public static void Postfix()
    {
        MainFile.TryDeferredConfigRegister();
    }
}

[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
public static class NGame_Ready_ConfigPreloadPatch
{
    public static void Prefix()
    {
        MainFile.TryDeferredConfigRegister();
    }
}

