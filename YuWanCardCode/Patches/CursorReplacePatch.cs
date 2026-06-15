using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using YuWanCard.Config;

namespace YuWanCard.Patches;

[HarmonyPatch]
public static class CursorReplacePatch
{
    private const string CursorSubPath = "ui/cursor_default.png";
    private const string CursorTiltedSubPath = "ui/cursor_tilted.png";

    private const int BaseCursorSize = 64;

    private static readonly FieldInfo? CursorNotTiltedField =
        AccessTools.Field(typeof(NCursorManager), "_cursorNotTilted");
    private static readonly FieldInfo? CursorTiltedField =
        AccessTools.Field(typeof(NCursorManager), "_cursorTilted");
    private static readonly FieldInfo? LastSetCursorField =
        AccessTools.Field(typeof(NCursorManager), "_lastSetCursor");

    private static readonly MethodInfo? UpdateCursorMethod =
        AccessTools.Method(typeof(NCursorManager), "UpdateCursor");

    private static Image? _cachedCursor;
    private static Image? _cachedCursorTilted;
    private static double _cachedScale = -1;

    private static Image? _originalNotTilted;
    private static Image? _originalTilted;
    private static bool _capturedOriginals;

    private static NCursorManager? _activeManager;

    [HarmonyPatch(typeof(NCursorManager), "_EnterTree")]
    [HarmonyPostfix]
    public static void OnEnterTree(NCursorManager __instance)
    {
        _activeManager = __instance;
        _capturedOriginals = false;
        RefreshCursor();
    }

    public static void RefreshCursor()
    {
        var manager = _activeManager;
        if (manager == null || !GodotObject.IsInstanceValid(manager))
        {
            _activeManager = null;
            return;
        }

        if (CursorNotTiltedField == null || CursorTiltedField == null)
            return;

        try
        {
            CaptureOriginals(manager);

            if (YuWanCardConfig.EnableCustomCursor)
            {
                var scale = GetClampedScale();
                if (Math.Abs(scale - _cachedScale) > 0.0001)
                {
                    _cachedCursor = null;
                    _cachedCursorTilted = null;
                    _cachedScale = scale;
                }

                var notTilted = LoadCursorImage(CursorSubPath, ref _cachedCursor, scale);
                var tilted = LoadCursorImage(CursorTiltedSubPath, ref _cachedCursorTilted, scale);
                if (notTilted != null && tilted != null)
                {
                    CursorNotTiltedField.SetValue(manager, notTilted);
                    CursorTiltedField.SetValue(manager, tilted);
                }
            }
            else
            {
                CursorNotTiltedField.SetValue(manager, _originalNotTilted);
                CursorTiltedField.SetValue(manager, _originalTilted);
            }

            LastSetCursorField?.SetValue(manager, null);
            UpdateCursorMethod?.Invoke(manager, null);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"应用自定义鼠标指针失败: {ex.Message}");
        }
    }

    private static void CaptureOriginals(NCursorManager manager)
    {
        if (_capturedOriginals)
            return;

        _originalNotTilted = CursorNotTiltedField!.GetValue(manager) as Image;
        _originalTilted = CursorTiltedField!.GetValue(manager) as Image;
        _capturedOriginals = true;
    }

    private static double GetClampedScale()
    {
        var scale = YuWanCardConfig.CursorScale;
        if (double.IsNaN(scale) || scale <= 0)
            return 1.0;
        return Math.Clamp(scale, 0.1, 10.0);
    }

    private static Image? LoadCursorImage(string subPath, ref Image? cache, double scale)
    {
        if (cache != null)
            return cache;

        var path = AssetPathHelper.GetImagePath(typeof(CursorReplacePatch), subPath);
        if (!ResourceLoader.Exists(path))
            return null;

        var texture = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
        if (texture == null)
        {
            MainFile.Logger.Warn($"未找到自定义鼠标指针资源: {path}");
            return null;
        }

        var image = texture.GetImage();
        if (image == null)
            return null;

        int target = Math.Clamp((int)Math.Round(BaseCursorSize * scale), 1, 256);
        int w = image.GetWidth();
        int h = image.GetHeight();
        if (w > 0 && h > 0 && (w != target || h != target))
        {
            float ratio = (float)target / Math.Max(w, h);
            int newW = Math.Clamp((int)Math.Round(w * ratio), 1, 256);
            int newH = Math.Clamp((int)Math.Round(h * ratio), 1, 256);
            image.Resize(newW, newH, Image.Interpolation.Lanczos);
        }

        cache = image;
        return cache;
    }
}
