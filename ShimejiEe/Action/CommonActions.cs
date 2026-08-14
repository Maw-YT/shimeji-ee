using GroupFinity.Mascot.Animation;
using GroupFinity.Mascot.Exception;
using GroupFinity.Mascot.I18n;
using GroupFinity.Mascot.Script;

namespace GroupFinity.Mascot.Action;

public class Stay : BorderedAction
{
    public Stay(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }

    protected override void tick()
    {
        base.tick();
        if (getBorder() != null && !getBorder()!.isOn(getMascot().anchor))
            throw new LostGroundException();
        getAnimation()?.next(getMascot(), getTime());
    }
}

public class Animate : BorderedAction
{
    public Animate(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }

    protected override void tick()
    {
        base.tick();
        if (getBorder() != null && !getBorder()!.isOn(getMascot().anchor))
            throw new LostGroundException();
        getAnimation()?.next(getMascot(), getTime());
    }

    public override bool hasNext()
    {
        var animation = getAnimation();
        var intime = animation != null && getTime() < animation.getDuration();
        return base.hasNext() && intime;
    }
}

public class Sequence : ComplexAction
{
    public Sequence(PropertiesBundle schema, VariableMap paramsMap, params Action[] actions)
        : base(schema, paramsMap, actions) { }

    public override bool hasNext()
    {
        seek();
        return base.hasNext();
    }

    protected override void setCurrentAction(int currentAction)
        => base.setCurrentAction(isLoop() ? currentAction % getActions().Length : currentAction);

    private bool isLoop() => evalBool(getSchema().getString("Loop"), false);
}

public class Select : ComplexAction
{
    public Select(PropertiesBundle schema, VariableMap paramsMap, params Action[] actions)
        : base(schema, paramsMap, actions) { }
}

public class Look : InstantAction
{
    public Look(PropertiesBundle schema, VariableMap paramsMap) : base(schema, paramsMap) { }

    protected override void apply()
        => getMascot().lookRight = evalBool(getSchema().getString("LookRight"), !getMascot().lookRight);
}

public class Offset : InstantAction
{
    public Offset(PropertiesBundle schema, VariableMap paramsMap) : base(schema, paramsMap) { }

    protected override void apply()
    {
        getMascot().anchor = new ScriptPoint(
            getMascot().anchor.x + evalInt(getSchema().getString("X"), 0),
            getMascot().anchor.y + evalInt(getSchema().getString("Y"), 0));
    }
}

public class Jump : ActionBase
{
    public Jump(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }

    public override bool hasNext()
    {
        var targetX = evalInt(getSchema().getString("TargetX"), 0);
        var targetY = evalInt(getSchema().getString("TargetY"), 0);
        var distanceX = targetX - getMascot().anchor.x;
        var distanceY = targetY - getMascot().anchor.y - Math.Abs(distanceX) / 2.0;
        var distance = Math.Sqrt(distanceX * distanceX + distanceY * distanceY);
        return base.hasNext() && distance != 0;
    }

    protected override void tick()
    {
        var targetX = evalInt(getSchema().getString("TargetX"), 0);
        var targetY = evalInt(getSchema().getString("TargetY"), 0);
        getMascot().lookRight = getMascot().anchor.x < targetX;
        var distanceX = targetX - getMascot().anchor.x;
        var distanceY = targetY - getMascot().anchor.y - Math.Abs(distanceX) / 2.0;
        var distance = Math.Sqrt(distanceX * distanceX + distanceY * distanceY);
        var velocity = evalDouble(getSchema().getString("VelocityParam"), 20.0);
        if (distance != 0)
        {
            var velocityX = (int)(velocity * distanceX / distance);
            var velocityY = (int)(velocity * distanceY / distance);
            putVariable(getSchema().getString("VelocityX"), velocity * distanceX / distance);
            putVariable(getSchema().getString("VelocityY"), velocity * distanceY / distance);
            getMascot().anchor = new ScriptPoint(getMascot().anchor.x + velocityX, getMascot().anchor.y + velocityY);
            getAnimation()?.next(getMascot(), getTime());
        }
        if (distance <= velocity)
            getMascot().anchor = new ScriptPoint(targetX, targetY);
    }
}

public class Dragged : ActionBase
{
    private double footX, footDx;
    private int timeToRegist;
    private double scaling;

    public Dragged(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }

    public override void init(Mascot mascot)
    {
        base.init(mascot);
        scaling = double.Parse(Main.getInstance().getProperties().getProperty("Scaling", "1.0"), System.Globalization.CultureInfo.InvariantCulture);
        footX = getEnvironment().getCursor().getX() + (int)Math.Round(getOffsetX() * scaling);
        timeToRegist = 250;
        putVariable(getSchema().getString("FootX"), footX);
        putVariable(getSchema().getString("FootDX"), 0.0);
    }

    public override bool hasNext() => base.hasNext() && getTime() < timeToRegist;

    protected override void tick()
    {
        getMascot().lookRight = false;
        getMascot().dragging = true;
        getEnvironment().refreshWorkArea();
        var cursor = getEnvironment().getCursor();
        var offsetX = (int)Math.Round(getOffsetX() * scaling);
        var offsetY = (int)Math.Round(getOffsetY() * scaling);
        if (getOffsetType() == getSchema().getString("Origin") && getMascot().getImage() != null)
        {
            offsetX = 0 - offsetX + getMascot().getImage()!.getCenter().X;
            offsetY = 0 - offsetY + getMascot().getImage()!.getCenter().Y;
        }
        if (Math.Abs(cursor.getX() - getMascot().anchor.x + offsetX) >= 5)
            setTime(0);
        var newX = cursor.getX();
        footDx = (footDx + ((newX - footX) * 0.1)) * 0.8;
        footX += footDx;
        putVariable(getSchema().getString("FootDX"), footDx);
        putVariable(getSchema().getString("FootX"), footX);
        getAnimation()?.next(getMascot(), getTime());
        getMascot().anchor = new ScriptPoint(cursor.getX() + offsetX, cursor.getY() + offsetY);
        if (getTime() == timeToRegist - 1 && Random.Shared.NextDouble() >= 0.1)
            timeToRegist++;
    }

    private int getOffsetX() => evalInt(getSchema().getString("OffsetX"), 0);
    private int getOffsetY() => evalInt(getSchema().getString("OffsetY"), 120);
    private string getOffsetType() => evalString(getSchema().getString("OffsetType"), "ImageAnchor");
}

public class Mute : InstantAction
{
    public Mute(PropertiesBundle schema, VariableMap paramsMap) : base(schema, paramsMap) { }
    protected override void apply() { }
}

public class SelfDestruct : InstantAction
{
    public SelfDestruct(PropertiesBundle schema, VariableMap paramsMap) : base(schema, paramsMap) { }
    protected override void apply() => getMascot().dispose();
}

public class Turn : Animate
{
    public Turn(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }
}

public class MoveWithTurn : Move
{
    public MoveWithTurn(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }
}

public class ComplexJump : Jump
{
    public ComplexJump(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }
}

public class ComplexMove : Move
{
    public ComplexMove(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }
}
