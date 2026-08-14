using GroupFinity.Mascot.Animation;
using GroupFinity.Mascot.Environment;
using GroupFinity.Mascot.Exception;
using GroupFinity.Mascot.Hotspot;
using GroupFinity.Mascot.I18n;
using GroupFinity.Mascot.Script;

namespace GroupFinity.Mascot.Action;

public abstract class ActionBase : Action
{
    public const string PARAMETER_DURATION = "Duration";
    public const string PARAMETER_CONDITION = "Condition";
    public const string PARAMETER_DRAGGABLE = "Draggable";
    public const string PARAMETER_AFFORDANCE = "Affordance";

    private Mascot? mascot;
    private int startTime;
    private readonly List<Animation.Animation> animations;
    private readonly VariableMap variables;
    private readonly PropertiesBundle schema;

    protected ActionBase(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
    {
        this.schema = schema;
        this.animations = animations;
        variables = context;
    }

    public override string ToString()
    {
        try { return "Action (" + GetType().Name + "," + getName() + ")"; }
        catch { return "Action (" + GetType().Name + ",)"; }
    }

    public virtual void init(Mascot mascot)
    {
        setMascot(mascot);
        setTime(0);
        getVariables().put("mascot", mascot);
        getVariables().put("action", this);
        getVariables().init();
        foreach (var animation in animations)
            animation.init();
    }

    public virtual void next()
    {
        initFrame();
        if (getMascot().getAffordances().Count > 0)
            getMascot().getAffordances().Clear();
        if (!string.IsNullOrWhiteSpace(getAffordance()))
            getMascot().getAffordances().Add(getAffordance());
        refreshHotspots();
        tick();
    }

    private void initFrame()
    {
        getVariables().initFrame();
        foreach (var animation in getAnimations())
            animation.initFrame();
    }

    protected List<Animation.Animation> getAnimations() => animations;
    protected abstract void tick();

    public virtual bool hasNext()
    {
        var effective = isEffective();
        var intime = getTime() < getDuration();
        return effective && intime;
    }

    protected void refreshHotspots()
    {
        getMascot().getHotspots().Clear();
        try
        {
            var animation = getAnimation();
            if (animation != null)
            {
                foreach (var hotspot in animation.getHotspots())
                    getMascot().getHotspots().Add(hotspot);
            }
        }
        catch (VariableException)
        {
            getMascot().getHotspots().Clear();
        }
    }

    public virtual bool isDraggable() => evalBool(schema.getString(PARAMETER_DRAGGABLE), true);
    private bool isEffective() => evalBool(schema.getString(PARAMETER_CONDITION), true);
    private int getDuration()
    {
        var raw = evalInt(schema.getString(PARAMETER_DURATION), int.MaxValue);
        if (raw >= int.MaxValue / 4)
            return raw;
        var scale = 0.5;
        var text = Main.getInstance().getProperties().getProperty("DurationScale", "0.5");
        if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            scale = Math.Clamp(parsed, 0.15, 1.0);
        return Math.Max(1, (int)Math.Round(raw * scale));
    }
    protected string getAffordance() => evalString(schema.getString(PARAMETER_AFFORDANCE), "");

    private void setMascot(Mascot mascot) => this.mascot = mascot;
    protected Mascot getMascot() => mascot!;
    protected int getTime() => getMascot().getTime() - startTime;
    protected void setTime(int time) => startTime = getMascot().getTime() - time;
    private string? getName() => evalString(schema.getString("Name"), null!);

    protected virtual Animation.Animation? getAnimation()
    {
        foreach (var animation in getAnimations())
        {
            if (animation.isEffective(getVariables()))
                return animation;
        }
        return null;
    }

    protected VariableMap getVariables() => variables;

    protected void putVariable(string key, object? value)
    {
        lock (getVariables())
            getVariables().put(key, value);
    }

    protected T eval<T>(string name, T defaultValue)
    {
        lock (getVariables())
        {
            if (getVariables().getRawMap().TryGetValue(name, out var variable) && variable != null)
            {
                var raw = variable.get(getVariables());
                if (raw == null) return defaultValue;
                if (raw is T t) return t;
                try { return (T)Convert.ChangeType(raw, typeof(T)); }
                catch { return defaultValue; }
            }
        }
        return defaultValue;
    }

    protected int evalInt(string name, int defaultValue)
    {
        try
        {
            var v = eval<object>(name, defaultValue);
            if (v == null) return defaultValue;
            if (v is double d)
                return double.IsFinite(d) ? Convert.ToInt32(Math.Clamp(d, int.MinValue + 1, int.MaxValue)) : defaultValue;
            return Convert.ToInt32(v);
        }
        catch
        {
            return defaultValue;
        }
    }

    protected double evalDouble(string name, double defaultValue)
    {
        var v = eval<object>(name, defaultValue);
        return v == null ? defaultValue : Convert.ToDouble(v);
    }

    protected bool evalBool(string name, bool defaultValue)
    {
        var v = eval<object>(name, defaultValue);
        return v == null ? defaultValue : Convert.ToBoolean(v);
    }

    protected string evalString(string name, string defaultValue)
    {
        var v = eval<object>(name, defaultValue);
        return v?.ToString() ?? defaultValue;
    }

    protected MascotEnvironment getEnvironment() => getMascot().environment;
    protected PropertiesBundle getSchema() => schema;
}
