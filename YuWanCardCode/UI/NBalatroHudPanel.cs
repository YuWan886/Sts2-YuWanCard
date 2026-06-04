using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;

namespace YuWanCard.UI;

public partial class NBalatroHudPanel : PanelContainer
{
    private const float DragThreshold = 10f;
    private const float Margin = 16f;

    private static bool _isOpen = true;

    private NJokerSlotBar? _jokerBar;
    private bool _isPointerDown;
    private bool _isDragging;
    private int _activePointerId = -1;
    private Vector2 _pointerStartPosition;
    private Vector2 _panelStartPosition;

    public static bool IsOpen => _isOpen;

    public static void ToggleOpen()
    {
        _isOpen = !_isOpen;
    }

    public override void _Ready()
    {
        Name = "YuWanBalatroHudPanel";
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.None;
        Size = new Vector2(420f, 122f);
        CustomMinimumSize = Size;

        StyleBoxFlat panelStyle = new()
        {
            BgColor = new Color(0.09f, 0.08f, 0.07f, 0.88f),
            BorderColor = new Color(0.91f, 0.82f, 0.61f, 0.95f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomRight = 12,
            CornerRadiusBottomLeft = 12,
            ShadowColor = new Color(0f, 0f, 0f, 0.35f),
            ShadowSize = 5
        };
        AddThemeStyleboxOverride("panel", panelStyle);

        VBoxContainer root = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            OffsetLeft = 10f,
            OffsetTop = 8f,
            OffsetRight = -10f,
            OffsetBottom = -8f
        };
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 6);
        AddChild(root);

        Label titleLabel = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Text = Loc("YUWANCARD-BALATRO_HUD.title"),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        titleLabel.AddThemeColorOverride("font_color", new Color(0.96f, 0.88f, 0.68f));
        titleLabel.AddThemeFontSizeOverride("font_size", 14);
        root.AddChild(titleLabel);

        _jokerBar = new NJokerSlotBar
        {
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(0f, 80f)
        };
        root.AddChild(_jokerBar);

        Resized += ClampToViewport;
        CallDeferred(nameof(InitializePosition));
    }

    public override void _Process(double delta)
    {
        if (RunManager.Instance?.State is not RunState state)
        {
            Visible = false;
            return;
        }

        BalatroModifier? modifier = BalatroModifier.GetInstance(state);
        Visible = modifier != null && _isOpen;
        if (modifier == null)
        {
            return;
        }

        if (_jokerBar != null)
        {
            _jokerBar.Visible = true;
        }

        if (Visible && !_isDragging)
        {
            ClampToViewport();
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventScreenTouch touch:
                if (touch.Pressed)
                {
                    BeginPointer(touch.Index, touch.Position);
                }
                else if (_isPointerDown && touch.Index == _activePointerId)
                {
                    EndPointer(touch.Position);
                }
                AcceptEvent();
                break;

            case InputEventScreenDrag drag when _isPointerDown && drag.Index == _activePointerId:
                UpdateDrag(drag.Position);
                AcceptEvent();
                break;

            case InputEventMouseButton mouseButton
                when !ShouldIgnoreMouseInput() && mouseButton.ButtonIndex == MouseButton.Left:
                if (mouseButton.Pressed)
                {
                    BeginPointer(-1, mouseButton.Position);
                }
                else if (_isPointerDown && _activePointerId == -1)
                {
                    EndPointer(mouseButton.Position);
                }
                AcceptEvent();
                break;

            case InputEventMouseMotion mouseMotion
                when !ShouldIgnoreMouseInput() && _isPointerDown && _activePointerId == -1:
                UpdateDrag(mouseMotion.Position);
                AcceptEvent();
                break;
        }
    }

    private static bool ShouldIgnoreMouseInput()
    {
        return OS.HasFeature("android");
    }

    private void InitializePosition()
    {
        Position = new Vector2(36f, 36f);
        ClampToViewport();
    }

    private void BeginPointer(int pointerId, Vector2 pointerPosition)
    {
        _isPointerDown = true;
        _isDragging = false;
        _activePointerId = pointerId;
        _pointerStartPosition = pointerPosition;
        _panelStartPosition = Position;
    }

    private void UpdateDrag(Vector2 pointerPosition)
    {
        Vector2 delta = pointerPosition - _pointerStartPosition;
        if (!_isDragging && delta.Length() >= DragThreshold)
        {
            _isDragging = true;
        }

        if (!_isDragging)
        {
            return;
        }

        Position = _panelStartPosition + delta;
        ClampToViewport();
    }

    private void EndPointer(Vector2 pointerPosition)
    {
        _isPointerDown = false;
        _isDragging = false;
        _activePointerId = -1;
        if ((pointerPosition - _pointerStartPosition).Length() >= DragThreshold)
        {
            ClampToViewport();
        }
    }

    private void ClampToViewport()
    {
        Vector2 viewportSize = GetViewportRect().Size;
        float maxX = Mathf.Max(Margin, viewportSize.X - Size.X - Margin);
        float maxY = Mathf.Max(Margin, viewportSize.Y - Size.Y - Margin);
        Position = new Vector2(
            Mathf.Clamp(Position.X, Margin, maxX),
            Mathf.Clamp(Position.Y, Margin, maxY));
    }

    private static string Loc(string key)
    {
        return new LocString("gameplay_ui", key).GetFormattedText();
    }
}
