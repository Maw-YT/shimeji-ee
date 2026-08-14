using GroupFinity.Mascot.Animation;
using GroupFinity.Mascot.I18n;
using GroupFinity.Mascot.Script;

namespace GroupFinity.Mascot.Action;

public abstract class InstantAction : ActionBase
{
    protected InstantAction(PropertiesBundle schema, VariableMap paramsMap)
        : base(schema, new List<Animation.Animation>(), paramsMap) { }

    public sealed override void init(Mascot mascot)
    {
        base.init(mascot);
        if (base.hasNext())
            apply();
    }

    protected abstract void apply();
    public sealed override bool hasNext() => false;
    protected sealed override void tick() { }
}
