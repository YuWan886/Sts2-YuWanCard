namespace YuWanCard.Core.Registration;

/// <summary>
/// Auto-registers an event model. The canonical instance created during
/// ModelDb.Init is registered with CustomEventRegistry.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RegisterEventAttribute : Attribute;

/// <summary>
/// Auto-registers a boss encounter into a target act's boss pool and discovery order.
/// Intended for EncounterModel types whose RoomType is Boss.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class RegisterBossAttribute : Attribute
{
    public Type ActType { get; }

    public bool IncludeInDiscoveryOrder { get; }

    public RegisterBossAttribute(Type actType, bool includeInDiscoveryOrder = true)
    {
        ActType = actType;
        IncludeInDiscoveryOrder = includeInDiscoveryOrder;
    }
}

/// <summary>
/// Auto-registers an ancient model. The canonical instance created during
/// ModelDb.Init is registered with CustomAncientRegistry.
/// Replaces the need for constructor-based registration in YuWanAncientModel.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RegisterAncientAttribute : Attribute;

/// <summary>
/// Auto-registers an orb model with the game's orb system.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RegisterOrbAttribute : Attribute;

/// <summary>
/// Auto-registers a monster model.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RegisterMonsterAttribute : Attribute;

/// <summary>
/// Auto-registers an enchantment model.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RegisterEnchantmentAttribute : Attribute;

/// <summary>
/// Auto-registers a singleton model.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RegisterSingletonAttribute : Attribute;

/// <summary>
/// Auto-registers a character model via ModelDbCharactersPatch.Register.
/// Characters implementing IYuWanCharacter are auto-detected as a fallback.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RegisterCharacterAttribute : Attribute;
