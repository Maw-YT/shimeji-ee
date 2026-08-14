using GroupFinity.Mascot.Exception;

namespace GroupFinity.Mascot.Script;

public abstract class Variable
{
    public static Variable parse(string? source)
    {
        if (source == null)
            return new Constant(null);
        if (source.StartsWith("${") && source.EndsWith('}'))
            return new Script(source[2..^1], false);
        if (source.StartsWith("#{") && source.EndsWith('}'))
            return new Script(source[2..^1], true);
        return new Constant(parseConstant(source));
    }

    private static object? parseConstant(string source)
    {
        if (source == "null") return null;
        if (source == "true") return true;
        if (source == "false") return false;
        if (double.TryParse(source, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d))
            return d;
        return source;
    }

    public abstract void init();
    public abstract void initFrame();
    public abstract object? get(VariableMap variables);
}

public sealed class Constant : Variable
{
    private readonly object? value;
    public Constant(object? value) { this.value = value; }
    public override void init() { }
    public override void initFrame() { }
    public override object? get(VariableMap variables) => value;
}
