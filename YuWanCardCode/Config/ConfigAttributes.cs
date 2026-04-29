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
