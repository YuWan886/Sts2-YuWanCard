namespace YuWanCard.Core.Interop;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class InteropTargetAttribute : Attribute
{
    public string? Type { get; }
    public string? Name { get; }

    public InteropTargetAttribute(string type, string? name = null)
    {
        Type = type;
        Name = name;
    }

    public InteropTargetAttribute(string? name = null)
    {
        Name = name;
    }
}
