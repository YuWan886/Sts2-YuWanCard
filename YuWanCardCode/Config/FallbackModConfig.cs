using System.Reflection;
using Godot;

namespace YuWanCard.Config;

/// <summary>
/// Fallback config base class used when BaseLib is not available at runtime.
/// Provides minimal implementations of the BaseLib ModConfig API so that
/// YuWanCardConfig can compile without BaseLib.dll present.
/// When BaseLib IS available, the reflection-based RegisterConfig in MainFile
/// will register this as if it were a real ModConfig.
/// </summary>
public class FallbackModConfig
{
    public event Action? ConfigChanged;

    /// <summary>
    /// Mirrors BaseLib.Config.ModConfig.HasSettings().
    /// Checks if there are any static, readable/writable properties on the subclass.
    /// </summary>
    public bool HasSettings()
    {
        var type = GetType();
        return type.GetProperties().Any(p =>
            p.CanRead && p.CanWrite && p.GetMethod?.IsStatic == true);
    }

    /// <summary>
    /// Injected by ModConfigRegistry.Register when BaseLib is available.
    /// </summary>
    public string? ModId { get; set; }

    protected void Changed()
    {
        ConfigChanged?.Invoke();
    }

    public virtual void SetupConfigUI(Control container) { }

    public void Save() { }
}

/// <summary>
/// Fallback SimpleModConfig extending FallbackModConfig with NOOP UI helpers.
/// </summary>
public class FallbackSimpleModConfig : FallbackModConfig
{
    public override void SetupConfigUI(Control container) { }

    protected void GenerateOptionsForAllProperties(Control container) { }

    protected Control CreateSectionHeader(string name, bool collapsible = false, bool collapsed = false) => new();
    protected Control CreateCollapsibleSection(string name, bool collapsed = false, bool startVisible = true) => new();

    protected Control CreateToggleOption(PropertyInfo prop, bool startVisible = true) => new();
    protected Control CreateSliderOption(PropertyInfo prop, bool startVisible = true) => new();
    protected Control CreateDropdownOption(PropertyInfo prop, bool startVisible = true) => new();
    protected Control CreateLineEditOption(PropertyInfo prop, bool startVisible = true) => new();
    protected Control CreateColorPickerOption(PropertyInfo prop, bool startVisible = true) => new();

    protected Control CreateButton(string key, string label, Action action, bool startVisible = true) => new();

    protected void SetupFocusNeighbors(Control container) { }
}
