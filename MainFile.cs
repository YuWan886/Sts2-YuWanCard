using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BaseLib.Config;
using BaseLib.Extensions;
using BaseLib.Utils.NodeFactories;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using YuWanCard.Config;
using YuWanCard.Patches;
using YuWanCard.Utils;

namespace YuWanCard;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "YuWanCard";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static YuWanCardConfig? Config { get; private set; }

    private const string PigVisualsPath = "res://YuWanCard/scenes/characters/pig.tscn";
    private const string PigMerchantPath = "res://YuWanCard/scenes/characters/pig_merchant.tscn";

#pragma warning disable CA2255
    [ModuleInitializer]
    internal static void ModuleInit()
#pragma warning restore CA2255
    {
        RegisterBaseLibAssemblyResolve();
    }

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.TryPatchAll(Assembly.GetExecutingAssembly());
        EndlessModePatch.ApplyMapPointTypeCountsPatches(harmony);
        
        Config = new YuWanCardConfig();
        ModConfigRegistry.Register(ModId, Config);
        Config.ConfigChanged += OnConfigChanged;

        NodeFactory.Init();
        RegisterSceneConversions();
        
        VfxUtils.PreloadScenes(
            "res://YuWanCard/scenes/vfx/vfx_blood_wheel_eye.tscn",
            "res://YuWanCard/scenes/vfx/vfx_black_hole.tscn",
            "res://YuWanCard/scenes/vfx/vfx_glitch.tscn",
            "res://YuWanCard/scenes/vfx/vfx_glass_shatter.tscn",
            "res://YuWanCard/scenes/vfx/vfx_matrix_rain.tscn"
        );

        VfxUtils.PreloadFrames("res://YuWanCard/images/vfx/blood_wheel_eye/blood_wheel_eye", 48);

        Logger.Info("YuWanCard initialized");
    }

    private static void RegisterSceneConversions()
    {
        NodeFactory.RegisterSceneType<NCreatureVisuals>(PigVisualsPath);
        NodeFactory.RegisterSceneType<NMerchantCharacter>(PigMerchantPath);
    }

    private static void OnConfigChanged(object? sender, EventArgs e)
    {
        
    }

    private static void RegisterBaseLibAssemblyResolve()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var assemblyName = new AssemblyName(args.Name);
            if (assemblyName.Name == "BaseLib")
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "BaseLib")
                    {
                        return asm;
                    }
                }
            }
            return null;
        };
    }
}
