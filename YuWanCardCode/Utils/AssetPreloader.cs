using Godot;

namespace YuWanCard.Utils;

/// <summary>
/// Handles preloading of mod assets (VFX scenes, frame sequences, textures)
/// at startup to reduce first-use stutter during gameplay.
/// </summary>
public static class AssetPreloader
{
    private const string VfxScenePath = "res://YuWanCard/scenes/vfx/";
    private const string ImagePath = "res://YuWanCard/images/";

    public static void Preload()
    {
        PreloadVfxScenes();
        PreloadVfxFrames();
        PreloadTextures();
    }

    private static void PreloadVfxScenes()
    {
        VfxUtils.PreloadScenes(
            $"{VfxScenePath}vfx_blood_wheel_eye.tscn",
            $"{VfxScenePath}vfx_black_hole.tscn",
            $"{VfxScenePath}vfx_glitch.tscn",
            $"{VfxScenePath}vfx_glass_shatter.tscn",
            $"{VfxScenePath}vfx_matrix_rain.tscn"
        );
    }

    private static void PreloadVfxFrames()
    {
        VfxUtils.PreloadFrames($"{ImagePath}vfx/blood_wheel_eye/blood_wheel_eye", 48);
    }

    private static void PreloadTextures()
    {
        PreloadTexturesInternal(
            $"{ImagePath}characters/character_icon_pig.png",
            $"{ImagePath}powers/pig_doubt_power.png"
        );
    }

    private static void PreloadTexturesInternal(params string[] texturePaths)
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
                MainFile.Logger.Warn($"PreloadTextures: Texture not found: {path}");
            }
        }

        if (loadedCount > 0)
        {
            MainFile.Logger.Debug($"PreloadTextures: Preloaded {loadedCount} textures");
        }
    }
}
