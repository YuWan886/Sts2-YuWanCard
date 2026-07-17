using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using YuWanCard.Core.Patches;
using YuWanCard.Core.Registration;

namespace YuWanCard.Core.Persistence;

public sealed class SavedAttachedState<TKey, TValue> where TKey : class
{
    private readonly SavedAttachedStateRegistration<TKey, TValue> _registration;
    private readonly ConditionalWeakTable<TKey, Box> _table = [];
    private readonly Func<TKey, TValue> _valueFactory;

    public SavedAttachedState(string name, Func<TValue>? defaultValueFactory = null, int order = 0)
        : this(name, _ => defaultValueFactory != null ? defaultValueFactory() : default!, order)
    {
    }

    public SavedAttachedState(string name, Func<TKey, TValue>? valueFactory, int order = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        SavedAttachedStateRegistry.ValidateSupportedType(typeof(TValue));

        _valueFactory = valueFactory ?? (_ => default!);
        _registration = new(name, order, this);
        SavedAttachedStateRegistry.Register(_registration);
    }

    public TValue this[TKey key]
    {
        get => GetOrCreate(key);
        set => Set(key, value);
    }

    public bool ContainsKey(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _table.TryGetValue(key, out _);
    }

    public TValue GetOrCreate(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _table.GetValue(key, k => new(_valueFactory(k))).Value;
    }

    public TValue? GetValueOrDefault(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return TryGetValue(key, out var value) ? value : default;
    }

    public TValue GetValueOrDefault(TKey key, TValue defaultValue)
    {
        ArgumentNullException.ThrowIfNull(key);
        return TryGetValue(key, out var value) ? value : defaultValue;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_table.TryGetValue(key, out var box))
        {
            value = box.Value;
            return true;
        }

        value = default;
        return false;
    }

    public TValue Set(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        _table.Remove(key);
        _table.Add(key, new(value));
        return value;
    }

    public bool Remove(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _table.Remove(key);
    }

    private sealed class Box(TValue value)
    {
        public TValue Value { get; } = value;
    }

    private sealed class SavedAttachedStateRegistration<TSavedKey, TSavedValue>(
        string name,
        int order,
        SavedAttachedState<TSavedKey, TSavedValue> owner) : ISavedAttachedState where TSavedKey : class
    {
        public string Name { get; } = name;
        public int Order { get; } = order;
        public Type TargetType { get; } = typeof(TSavedKey);

        public bool Export(object model, SavedProperties props)
        {
            return owner.TryGetValue((TSavedKey)model, out var value)
                   && SavedAttachedStateRegistry.AddToProperties(props, Name, value);
        }

        public void Import(object model, SavedProperties props)
        {
            if (SavedAttachedStateRegistry.TryGetFromProperties<TSavedValue>(props, Name, out var value))
            {
                owner.Set((TSavedKey)model, value!);
            }
        }

        public void Clone(object source, object clone)
        {
            if (!owner.TryGetValue((TSavedKey)source, out var value))
            {
                return;
            }

            owner.Set((TSavedKey)clone, (TSavedValue)SavedAttachedStateRegistry.CloneValue(value)!);
        }
    }
}

internal interface ISavedAttachedState
{
    string Name { get; }
    int Order { get; }
    Type TargetType { get; }
    bool Export(object model, SavedProperties props);
    void Import(object model, SavedProperties props);
    void Clone(object source, object clone);
}

internal static class SavedAttachedStateRegistry
{
    private static readonly object SyncRoot = new();
    private static readonly List<ISavedAttachedState> RegisteredStates = [];
    private static readonly HashSet<string> RegisteredKeys = [];

    private static readonly HashSet<Type> SupportedTypes =
    [
        typeof(int),
        typeof(bool),
        typeof(string),
        typeof(ModelId),
        typeof(int[]),
        typeof(SerializableCard),
        typeof(SerializableCard[]),
        typeof(List<SerializableCard>)
    ];

    /// <summary>
    /// Runs type initializers that declare attached state so their property names reach
    /// SavedPropertiesTypeCache before external frameworks finalize its network ID table.
    /// </summary>
    internal static void RegisterAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var stateOwnerTypes = AssemblyScanner.GetLoadableTypes(assembly)
            .Where(DeclaresAttachedState)
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        int initializedCount = 0;
        foreach (Type type in stateOwnerTypes)
        {
            try
            {
                RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                initializedCount++;
            }
            catch (Exception ex)
            {
                MainFile.Logger.Warn(
                    $"SavedAttachedState: failed to initialize {type.FullName}: {ex.Message}");
            }
        }

        if (initializedCount > 0)
        {
            MainFile.Logger.Info(
                $"SavedAttachedState: registered state keys from {initializedCount} type(s)");
        }
    }

    private static bool DeclaresAttachedState(Type type)
    {
        return type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Any(field => field.FieldType.IsGenericType
                          && field.FieldType.GetGenericTypeDefinition() == typeof(SavedAttachedState<,>));
    }

    internal static void Register(ISavedAttachedState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        lock (SyncRoot)
        {
            string key = $"{state.TargetType.AssemblyQualifiedName}|{state.Name}";
            if (!RegisteredKeys.Add(key))
            {
                throw new InvalidOperationException(
                    $"SavedAttachedState is already registered for {state.TargetType.FullName}: {state.Name}");
            }

            RegisteredStates.Add(state);
            RegisteredStates.Sort(static (left, right) =>
            {
                int orderCompare = left.Order.CompareTo(right.Order);
                return orderCompare != 0 ? orderCompare : string.CompareOrdinal(left.Name, right.Name);
            });
        }

        SavedPropertiesTypeCachePatch.EnsurePropertyNameRegistered(state.Name);
    }

    internal static IReadOnlyList<ISavedAttachedState> GetStatesForModel(object model)
    {
        ArgumentNullException.ThrowIfNull(model);

        lock (SyncRoot)
        {
            return RegisteredStates
                .Where(state => state.TargetType.IsInstanceOfType(model))
                .ToArray();
        }
    }

    internal static void ExportAttachedStates(ref SavedProperties? properties, object model)
    {
        IReadOnlyList<ISavedAttachedState> states = GetStatesForModel(model);
        if (states.Count == 0)
        {
            return;
        }

        SavedProperties props = properties ?? new SavedProperties();
        bool added = false;
        foreach (ISavedAttachedState state in states)
        {
            if (state.Export(model, props))
            {
                added = true;
            }
        }

        if (properties == null && added)
        {
            properties = props;
        }
    }

    internal static void ImportAttachedStates(SavedProperties properties, object model)
    {
        foreach (ISavedAttachedState state in GetStatesForModel(model))
        {
            state.Import(model, properties);
        }
    }

    internal static void CloneAttachedStates(AbstractModel prototype, AbstractModel clone)
    {
        ArgumentNullException.ThrowIfNull(prototype);
        ArgumentNullException.ThrowIfNull(clone);

        if (ReferenceEquals(prototype, clone))
        {
            return;
        }

        foreach (ISavedAttachedState state in GetStatesForModel(prototype))
        {
            if (state.TargetType.IsInstanceOfType(clone))
            {
                state.Clone(prototype, clone);
            }
        }
    }

    internal static void ValidateSupportedType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (SupportedTypes.Contains(type) || type.IsEnum || (type.IsArray && type.GetElementType()?.IsEnum == true))
        {
            return;
        }

        throw new NotSupportedException(
            $"SavedAttachedState uses unsupported type {type.Name}. Only SavedProperties-compatible value types are supported.");
    }

    internal static bool AddToProperties(SavedProperties props, string name, object? value)
    {
        switch (value)
        {
            case null:
                return false;
            case int intValue:
                (props.ints ??= []).Add(new(name, intValue));
                return true;
            case bool boolValue:
                (props.bools ??= []).Add(new(name, boolValue));
                return true;
            case string stringValue:
                (props.strings ??= []).Add(new(name, stringValue));
                return true;
            case Enum enumValue:
                (props.ints ??= []).Add(new(name, Convert.ToInt32(enumValue)));
                return true;
            case ModelId modelId:
                (props.modelIds ??= []).Add(new(name, modelId));
                return true;
            case SerializableCard card:
                (props.cards ??= []).Add(new(name, card));
                return true;
            case int[] intArray:
                (props.intArrays ??= []).Add(new(name, intArray));
                return true;
            case Enum[] enumArray:
                (props.intArrays ??= []).Add(new(name, enumArray.Select(Convert.ToInt32).ToArray()));
                return true;
            case SerializableCard[] cardArray:
                (props.cardArrays ??= []).Add(new(name, cardArray));
                return true;
            case List<SerializableCard> cardList:
                (props.cardArrays ??= []).Add(new(name, cardList.ToArray()));
                return true;
            default:
                return false;
        }
    }

    internal static bool TryGetFromProperties<T>(SavedProperties props, string name, out T? value)
    {
        value = default;

        if (typeof(T) == typeof(int) || typeof(T).IsEnum)
        {
            var found = props.ints?.FirstOrDefault(property => property.name == name);
            if (found == null)
            {
                return false;
            }

            value = typeof(T).IsEnum
                ? (T)Enum.ToObject(typeof(T), found.Value.value)
                : (T)(object)found.Value.value;
            return true;
        }

        if (typeof(T) == typeof(bool))
        {
            var found = props.bools?.FirstOrDefault(property => property.name == name);
            if (found == null)
            {
                return false;
            }

            value = (T)(object)found.Value.value;
            return true;
        }

        if (typeof(T) == typeof(string))
        {
            var found = props.strings?.FirstOrDefault(property => property.name == name);
            if (found == null)
            {
                return false;
            }

            value = (T)(object)found.Value.value;
            return true;
        }

        if (typeof(T) == typeof(ModelId))
        {
            var found = props.modelIds?.FirstOrDefault(property => property.name == name);
            if (found == null)
            {
                return false;
            }

            value = (T)(object)found.Value.value;
            return true;
        }

        if (typeof(T) == typeof(int[]) || (typeof(T).IsArray && typeof(T).GetElementType()?.IsEnum == true))
        {
            var found = props.intArrays?.FirstOrDefault(property => property.name == name);
            if (found == null)
            {
                return false;
            }

            if (typeof(T).IsArray && typeof(T).GetElementType()?.IsEnum == true)
            {
                Type enumType = typeof(T).GetElementType()!;
                Array enumValues = Array.CreateInstance(enumType, found.Value.value.Length);
                for (int index = 0; index < found.Value.value.Length; index++)
                {
                    enumValues.SetValue(Enum.ToObject(enumType, found.Value.value[index]), index);
                }

                value = (T)(object)enumValues;
            }
            else
            {
                value = (T)(object)found.Value.value;
            }

            return true;
        }

        if (typeof(T) == typeof(SerializableCard))
        {
            var found = props.cards?.FirstOrDefault(property => property.name == name);
            if (found == null)
            {
                return false;
            }

            value = (T)(object)found.Value.value;
            return true;
        }

        if (typeof(T) != typeof(SerializableCard[]) && typeof(T) != typeof(List<SerializableCard>))
        {
            return false;
        }

        {
            var found = props.cardArrays?.FirstOrDefault(property => property.name == name);
            if (found == null)
            {
                return false;
            }

            value = typeof(T) == typeof(List<SerializableCard>)
                ? (T)(object)found.Value.value.ToList()
                : (T)(object)found.Value.value;
            return true;
        }
    }

    internal static object? CloneValue(object? value)
    {
        return value switch
        {
            null => null,
            int or bool or string or ModelId => value,
            Enum => value,
            int[] intArray => (int[])intArray.Clone(),
            Array enumArray when enumArray.GetType().GetElementType()?.IsEnum == true => enumArray.Clone(),
            SerializableCard card => CloneSerializableCard(card),
            SerializableCard[] cardArray => cardArray.Select(CloneSerializableCard).ToArray(),
            List<SerializableCard> cardList => cardList.Select(CloneSerializableCard).ToList(),
            _ => value
        };
    }

    private static SerializableCard CloneSerializableCard(SerializableCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return new SerializableCard
        {
            Id = card.Id,
            CurrentUpgradeLevel = card.CurrentUpgradeLevel,
            Enchantment = card.Enchantment == null ? null : CloneSerializableEnchantment(card.Enchantment),
            Props = card.Props == null ? null : CloneSavedProperties(card.Props),
            FloorAddedToDeck = card.FloorAddedToDeck
        };
    }

    private static SerializableEnchantment CloneSerializableEnchantment(SerializableEnchantment enchantment)
    {
        ArgumentNullException.ThrowIfNull(enchantment);
        return new SerializableEnchantment
        {
            Id = enchantment.Id,
            Amount = enchantment.Amount,
            Props = enchantment.Props == null ? null : CloneSavedProperties(enchantment.Props)
        };
    }

    private static SavedProperties CloneSavedProperties(SavedProperties props)
    {
        ArgumentNullException.ThrowIfNull(props);
        return new SavedProperties
        {
            ints = props.ints?.Select(static property => new SavedProperties.SavedProperty<int>(property.name, property.value)).ToList(),
            bools = props.bools?.Select(static property => new SavedProperties.SavedProperty<bool>(property.name, property.value)).ToList(),
            strings = props.strings?.Select(static property => new SavedProperties.SavedProperty<string>(property.name, property.value)).ToList(),
            intArrays = props.intArrays?.Select(static property =>
                new SavedProperties.SavedProperty<int[]>(property.name, (int[])property.value.Clone())).ToList(),
            modelIds = props.modelIds?.Select(static property => new SavedProperties.SavedProperty<ModelId>(property.name, property.value)).ToList(),
            cards = props.cards?.Select(static property =>
                new SavedProperties.SavedProperty<SerializableCard>(property.name, CloneSerializableCard(property.value))).ToList(),
            cardArrays = props.cardArrays?.Select(static property =>
                new SavedProperties.SavedProperty<SerializableCard[]>(property.name, property.value.Select(CloneSerializableCard).ToArray())).ToList()
        };
    }
}
