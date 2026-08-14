using GroupFinity.Mascot.Exception;

namespace GroupFinity.Mascot.Script;

public sealed class VariableMap : Dictionary<string, object?>
{
    public Dictionary<string, Variable> getRawMap() => rawMap;

    private readonly Dictionary<string, Variable> rawMap = new();

    public void init()
    {
        foreach (var v in rawMap.Values)
            v.init();
    }

    public void initFrame()
    {
        foreach (var v in rawMap.Values)
            v.initFrame();
    }

    public new object? this[string key]
    {
        get => rawMap.TryGetValue(key, out var v) ? v.get(this) : null;
        set => put(key, value);
    }

    public object? put(string key, object? value)
    {
        rawMap.TryGetValue(key, out var previous);
        if (value is Variable variable)
            rawMap[key] = variable;
        else
            rawMap[key] = new Constant(value);
        try { return previous?.get(this); }
        catch { return previous; }
    }

    public void putAll(IDictionary<string, string> constants)
    {
        foreach (var kv in constants)
            put(kv.Key, Variable.parse(kv.Value));
    }

    public IEnumerable<KeyValuePair<string, object?>> ResolvedEntries()
    {
        foreach (var kv in rawMap)
            yield return new KeyValuePair<string, object?>(kv.Key, kv.Value.get(this));
    }
}
