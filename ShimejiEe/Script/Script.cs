using GroupFinity.Mascot.Exception;
using Jint;
using Jint.Native;

namespace GroupFinity.Mascot.Script;

public sealed class Script : Variable
{
    private static readonly object EngineLock = new();
    private static readonly Engine Engine = new(options =>
    {
        options.TimeoutInterval(TimeSpan.FromMilliseconds(250));
        options.MaxStatements(50_000);
        options.LimitRecursion(64);
        options.Strict = false;
    });

    [ThreadStatic] private static HashSet<Script>? evaluating;

    private readonly string source;
    private readonly bool clearAtInitFrame;
    private object? value;

    public Script(string source, bool clearAtInitFrame)
    {
        this.source = source;
        this.clearAtInitFrame = clearAtInitFrame;
    }

    public override string ToString() => clearAtInitFrame ? "#{" + source + "}" : "${" + source + "}";

    public override void init() => value = null;

    public override void initFrame()
    {
        if (clearAtInitFrame)
            value = null;
    }

    public override object? get(VariableMap variables)
    {
        if (value != null)
            return value;
        evaluating ??= new HashSet<Script>();
        if (!evaluating.Add(this))
            return null;
        try
        {
            lock (EngineLock)
            {
                foreach (var kv in variables.getRawMap())
                {
                    if (ReferenceEquals(kv.Value, this))
                        continue;
                    if (kv.Value is Script other && evaluating.Contains(other) && other.value == null)
                        continue;
                    object? resolved;
                    try { resolved = kv.Value.get(variables); }
                    catch { continue; }
                    Engine.SetValue(kv.Key, resolved ?? JsValue.Undefined);
                }
                value = Unwrap(Engine.Evaluate(source));
            }
        }
        catch (System.Exception e)
        {
            throw new VariableException(Main.getInstance().getLanguageBundle().getString("ScriptEvaluationErrorMessage") + ": " + source, e);
        }
        finally
        {
            evaluating.Remove(this);
        }
        return value;
    }

    private static object? Unwrap(JsValue result)
    {
        if (result.IsNull() || result.IsUndefined())
            return null;
        if (result.IsBoolean())
            return result.AsBoolean();
        if (result.IsNumber())
            return result.AsNumber();
        if (result.IsString())
            return result.AsString();
        return result.ToObject();
    }
}
