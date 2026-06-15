namespace BaseLib.Config;

/// <summary>
/// Creates a new section in the ModConfig UI.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public class ConfigSectionAttribute : Attribute
{
    public string Name { get; }

    public ConfigSectionAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Show a tooltip for this setting on hover.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public class ConfigHoverTipAttribute : Attribute
{
    public bool Enabled { get; }

    public ConfigHoverTipAttribute(bool enabled = true)
    {
        Enabled = enabled;
    }
}

/// <summary>
/// Renders a numeric property as a slider with the given range, step, and label format.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ConfigSliderAttribute : Attribute
{
    public double Min { get; }
    public double Max { get; }
    public double Step { get; }
    public string? Format { get; }

    public ConfigSliderAttribute(double min, double max, double step = 1d, string? format = null)
    {
        Min = min;
        Max = max;
        Step = step;
        Format = format;
    }
}
