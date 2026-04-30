namespace YuWanCard.Core.Interop;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ModInteropAttribute : Attribute
{
    public string ModId { get; }
    public string? Type { get; }

    public ModInteropAttribute(string modId, string? type = null)
    {
        ModId = modId;
        Type = type;
    }
}
