using GroupFinity.Mascot.Animation;
using GroupFinity.Mascot.I18n;
using GroupFinity.Mascot.Script;

namespace GroupFinity.Mascot.Action;

public abstract class ComplexAction : ActionBase
{
    private readonly Action[] actions;
    private int currentAction;

    protected ComplexAction(PropertiesBundle schema, VariableMap paramsMap, params Action[] actions)
        : base(schema, new List<Animation.Animation>(), paramsMap)
    {
        if (actions.Length == 0)
            throw new ArgumentException("actions.length==0");
        this.actions = actions;
    }

    public override void init(Mascot mascot)
    {
        base.init(mascot);
        if (base.hasNext())
        {
            setCurrentAction(0);
            seek();
        }
    }

    protected void seek()
    {
        if (base.hasNext())
        {
            while (getCurrentAction() < getActions().Length)
            {
                if (getAction().hasNext())
                    break;
                setCurrentAction(getCurrentAction() + 1);
            }
        }
    }

    public override bool hasNext()
    {
        var inrange = getCurrentAction() < getActions().Length;
        return base.hasNext() && inrange && getAction().hasNext();
    }

    protected override void tick()
    {
        if (getAction().hasNext())
            getAction().next();
    }

    public override bool isDraggable()
    {
        if (currentAction < actions.Length && actions[currentAction] is ActionBase ab)
            return ab.isDraggable();
        return true;
    }

    protected virtual void setCurrentAction(int currentAction)
    {
        this.currentAction = currentAction;
        if (base.hasNext() && this.currentAction < getActions().Length)
            getAction().init(getMascot());
    }

    protected int getCurrentAction() => currentAction;
    protected Action[] getActions() => actions;
    protected Action getAction() => getActions()[getCurrentAction()];
}
