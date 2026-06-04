using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;
using YuWanCard.Modifiers;

namespace YuWanCard.UI;

public partial class NBalatroHudPanel : PanelContainer
{
    private const float DragThreshold = 6f;
    private const float Margin = 16f;
    private const float HorizontalPadding = 24f;
    private const float MinimumWidth = 320f;
    private const float MinimumHeight = 108f;

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
        Size = new Vector2(MinimumWidth, MinimumHeight);
        CustomMinimumSize = Size;
        AddThemeStyleboxOverride("panel", BalatroUiTheme.CreatePanelStyle());

        VBoxContainer root = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            OffsetLeft = 12f,
            OffsetTop = 10f,
            OffsetRight = -12f,
            OffsetBottom = -10f
        };
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 6);
        AddChild(root);

        Label titleLabel = BalatroUiTheme.CreateTextLabel(
            Loc("YUWANCARD-BALATRO_HUD.title"),
            14,
            BalatroUiTheme.Title);
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
            UpdatePanelSize();
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
                    AcceptEvent();
                }
                break;

            case InputEventMouseButton mouseButton
                when !ShouldIgnoreMouseInput() && mouseButton.ButtonIndex == MouseButton.Left:
                if (mouseButton.Pressed)
                {
                    BeginPointer(-1, mouseButton.Position);
                    AcceptEvent();
                }
                break;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isPointerDown)
        {
            return;
        }

        switch (@event)
        {
            case InputEventScreenDrag drag when drag.Index == _activePointerId:
                UpdateDrag(drag.Position, drag.Relative);
                GetViewport().SetInputAsHandled();
                break;

            case InputEventScreenTouch touch when !touch.Pressed && touch.Index == _activePointerId:
                EndPointer();
                GetViewport().SetInputAsHandled();
                break;

            case InputEventMouseMotion mouseMotion
                when !ShouldIgnoreMouseInput() && _activePointerId == -1:
                UpdateDrag(mouseMotion.Position, mouseMotion.Relative);
                if (_isDragging)
                {
                    GetViewport().SetInputAsHandled();
                }
                break;

            case InputEventMouseButton mouseButton
                when !ShouldIgnoreMouseInput() && mouseButton.ButtonIndex == MouseButton.Left && !mouseButton.Pressed && _activePointerId == -1:
                EndPointer();
                if (_isDragging)
                {
                    GetViewport().SetInputAsHandled();
                }
                break;
        }
    }

    private static bool ShouldIgnoreMouseInput()
    {
        return RuntimePlatform.IsMobileLike;
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

    private void UpdateDrag(Vector2 pointerPosition, Vector2 relativeMotion)
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

        if (delta.Length() <= DragThreshold + 0.01f)
        {
            Position = _panelStartPosition + delta;
        }
        else
        {
            Position += relativeMotion;
        }
        ClampToViewport();
    }

    private void EndPointer()
    {
        bool wasDragging = _isDragging;
        _isPointerDown = false;
        _isDragging = false;
        _activePointerId = -1;
        if (wasDragging)
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

    private void UpdatePanelSize()
    {
        if (_jokerBar == null)
        {
            return;
        }

        float targetWidth = Mathf.Max(MinimumWidth, _jokerBar.GetPreferredHudWidth() + HorizontalPadding);
        Vector2 targetSize = new(targetWidth, MinimumHeight);
        if (Size == targetSize && CustomMinimumSize == targetSize)
        {
            return;
        }

        Size = targetSize;
        CustomMinimumSize = targetSize;
    }
}
