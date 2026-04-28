using System.Runtime.CompilerServices;

namespace YuWanCard.Core.Utils;

/// <summary>
/// A basic wrapper around ConditionalWeakTable for storing per-instance data.
/// </summary>
public class SpireField<TKey, TVal> where TKey : class
{
    private readonly ConditionalWeakTable<TKey, object?> _table = new();
    private readonly Func<TKey, TVal?> _defaultVal;

    public SpireField(Func<TVal?> defaultVal) : this(_ => defaultVal()) { }

    public SpireField(Func<TKey, TVal?> defaultVal)
    {
        _defaultVal = defaultVal;
    }

    public TVal? Get(TKey obj)
    {
        if (_table.TryGetValue(obj, out var result)) return (TVal?)result;
        _table.Add(obj, result = _defaultVal(obj));
        return (TVal?)result;
    }

    public void Set(TKey obj, TVal? val)
    {
        _table.AddOrUpdate(obj, val);
    }

    public TVal? this[TKey obj]
    {
        get => Get(obj);
        set => Set(obj, value);
    }
}

/// <summary>
/// A SpireField with save/load support.
/// </summary>
public class SavedSpireField<TKey, TVal> : SpireField<TKey, TVal> where TKey : class
{
    public string Name { get; }

    public SavedSpireField(Func<TVal?> defaultVal, string name) : base(defaultVal)
    {
        Name = name;
    }

    public SavedSpireField(Func<TKey, TVal?> defaultVal, string name) : base(defaultVal)
    {
        Name = name;
    }
}
