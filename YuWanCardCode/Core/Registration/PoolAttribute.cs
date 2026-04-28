namespace YuWanCard.Core.Registration;

/// <summary>
/// Marks a card/relic/power class as belonging to a specific content pool.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class PoolAttribute : Attribute
{
    public Type PoolType { get; }

    public PoolAttribute(Type poolType)
    {
        PoolType = poolType;
    }
}
