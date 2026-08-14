using GroupFinity.Mascot.Animation;
using GroupFinity.Mascot.Exception;
using GroupFinity.Mascot.I18n;
using GroupFinity.Mascot.Script;

namespace GroupFinity.Mascot.Action;

public class Breed : Animate
{
    public Breed(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }

    public override void init(Mascot mascot)
    {
        base.init(mascot);
        if (getBornCount() < 1)
            throw new VariableException("BornCount must be positive");
    }

    protected override void tick()
    {
        base.tick();
        var animation = getAnimation();
        if (animation != null && getTime() == animation.getDuration() - 1 && isEnabled())
            breed();
    }

    protected bool isEnabled()
    {
        if (getBornTransient())
            return Main.getInstance().canSpawn(getMascot().imageSet, fromBreed: false, transient: true);
        var born = getBornMascot();
        var childType = Main.getInstance().tryGetConfiguration(born, out _) ? born : getMascot().imageSet;
        return Main.getInstance().canSpawn(childType, fromBreed: true, transient: false);
    }

    protected void breed()
    {
        var scaling = double.Parse(Main.getInstance().getProperties().getProperty("Scaling", "1.0"), System.Globalization.CultureInfo.InvariantCulture);
        var born = getBornMascot();
        var childType = Main.getInstance().tryGetConfiguration(born, out _) ? born : getMascot().imageSet;
        for (var index = 0; index < getBornCount(); index++)
        {
            if (!Main.getInstance().canSpawn(childType, fromBreed: !getBornTransient(), transient: getBornTransient()))
                break;
            var mascot = new Mascot(childType);
            if (getMascot().lookRight)
                mascot.anchor = new ScriptPoint(getMascot().anchor.x - (int)Math.Round(getBornX() * scaling), getMascot().anchor.y + (int)Math.Round(getBornY() * scaling));
            else
                mascot.anchor = new ScriptPoint(getMascot().anchor.x + (int)Math.Round(getBornX() * scaling), getMascot().anchor.y + (int)Math.Round(getBornY() * scaling));
            mascot.lookRight = getMascot().lookRight;
            try
            {
                mascot.setBehavior(Main.getInstance().getConfiguration(childType).buildBehavior(getBornBehaviour(), getMascot()));
                getMascot().getManager()?.add(mascot);
            }
            catch (System.Exception e)
            {
                Log.Severe("Failed to breed", e);
                Main.showError(Main.getInstance().getLanguageBundle().getString("FailedCreateNewShimejiErrorMessage"), e);
                mascot.dispose();
            }
        }
    }

    protected int getBornX() => evalInt(getSchema().getString("BornX"), 0);
    protected int getBornY() => evalInt(getSchema().getString("BornY"), 0);
    protected string getBornBehaviour() => evalString(getSchema().getString("BornBehaviour"), "");
    protected string getBornMascot() => evalString(getSchema().getString("BornMascot"), "");
    protected bool getBornTransient() => evalBool(getSchema().getString("BornTransient"), false);
    protected int getBornCount() => evalInt(getSchema().getString("BornCount"), 1);
}

public class BreedJump : Jump
{
    public BreedJump(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }
}

public class BreedMove : Move
{
    public BreedMove(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }
}

public class ThrowIE : Animate
{
    public ThrowIE(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }

    public override bool hasNext()
    {
        if (!bool.Parse(Main.getInstance().getProperties().getProperty("Throwing", "true")))
            return false;
        return base.hasNext() && getEnvironment().getActiveIE().isVisible();
    }

    protected override void tick()
    {
        base.tick();
        var activeIE = getEnvironment().getActiveIE();
        if (!activeIE.isVisible()) return;
        var vx = evalInt(getSchema().getString("InitialVX"), 32);
        var vy = evalInt(getSchema().getString("InitialVY"), -10);
        var gravity = evalDouble(getSchema().getString("Gravity"), 0.5);
        if (getMascot().lookRight)
            getEnvironment().moveActiveIE(new ScriptPoint(activeIE.getLeft() + vx, activeIE.getTop() + vy + (int)(getTime() * gravity)));
        else
            getEnvironment().moveActiveIE(new ScriptPoint(activeIE.getLeft() - vx, activeIE.getTop() + vy + (int)(getTime() * gravity)));
    }
}

public class WalkWithIE : Move
{
    public WalkWithIE(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }

    public override bool hasNext()
        => bool.Parse(Main.getInstance().getProperties().getProperty("Throwing", "true")) && base.hasNext();

    protected override void tick()
    {
        var activeIE = getEnvironment().getActiveIE();
        if (!activeIE.isVisible()) throw new LostGroundException();
        var offsetX = evalInt(getSchema().getString("IeOffsetX"), 0);
        var offsetY = evalInt(getSchema().getString("IeOffsetY"), 0);
        if (getMascot().lookRight)
        {
            if (getMascot().anchor.x - offsetX != activeIE.getLeft() || getMascot().anchor.y + offsetY != activeIE.getBottom())
                throw new LostGroundException();
        }
        else if (getMascot().anchor.x + offsetX != activeIE.getRight() || getMascot().anchor.y + offsetY != activeIE.getBottom())
            throw new LostGroundException();
        base.tick();
        if (activeIE.isVisible())
        {
            if (getMascot().lookRight)
                getEnvironment().moveActiveIE(new ScriptPoint(getMascot().anchor.x - offsetX, getMascot().anchor.y + offsetY - activeIE.getHeight()));
            else
                getEnvironment().moveActiveIE(new ScriptPoint(getMascot().anchor.x + offsetX - activeIE.getWidth(), getMascot().anchor.y + offsetY - activeIE.getHeight()));
        }
    }
}

public class FallWithIE : Fall
{
    public FallWithIE(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }

    public override bool hasNext()
        => bool.Parse(Main.getInstance().getProperties().getProperty("Throwing", "true")) && base.hasNext();

    protected override void tick()
    {
        var activeIE = getEnvironment().getActiveIE();
        if (!activeIE.isVisible()) throw new LostGroundException();
        var offsetX = evalInt(getSchema().getString("IeOffsetX"), 0);
        var offsetY = evalInt(getSchema().getString("IeOffsetY"), 0);
        if (getMascot().lookRight)
        {
            if (getMascot().anchor.x - offsetX != activeIE.getLeft() || getMascot().anchor.y + offsetY != activeIE.getBottom())
                throw new LostGroundException();
        }
        else if (getMascot().anchor.x + offsetX != activeIE.getRight() || getMascot().anchor.y + offsetY != activeIE.getBottom())
            throw new LostGroundException();
        base.tick();
        if (activeIE.isVisible())
        {
            if (getMascot().lookRight)
                getEnvironment().moveActiveIE(new ScriptPoint(getMascot().anchor.x - offsetX, getMascot().anchor.y + offsetY - activeIE.getHeight()));
            else
                getEnvironment().moveActiveIE(new ScriptPoint(getMascot().anchor.x + offsetX - activeIE.getWidth(), getMascot().anchor.y + offsetY - activeIE.getHeight()));
        }
    }
}

public class Transform : Animate
{
    public Transform(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }

    protected override void tick()
    {
        base.tick();
        var animation = getAnimation();
        if (animation != null && (getTime() == animation.getDuration() - 1 || animation.getDuration() == 1) &&
            bool.Parse(Main.getInstance().getProperties().getProperty("Transformation", "true")))
        {
            var transformSet = evalString(getSchema().getString("TransformMascot"), "");
            var childType = Main.getInstance().tryGetConfiguration(transformSet, out _) ? transformSet : getMascot().imageSet;
            try
            {
                getMascot().imageSet = childType;
                getMascot().setBehavior(Main.getInstance().getConfiguration(childType).buildBehavior(evalString(getSchema().getString("TransformBehaviour"), ""), getMascot()));
            }
            catch (System.Exception e)
            {
                Log.Severe("Transform failed", e);
                Main.showError(Main.getInstance().getLanguageBundle().getString("FailedCreateNewShimejiErrorMessage"), e);
            }
        }
    }
}

public class Interact : Animate
{
    public Interact(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }

    public override bool hasNext()
        => base.hasNext() && (getMascot().getManager()?.hasOverlappingMascotsAtPoint(getMascot().anchor) ?? false);

    protected override void tick()
    {
        base.tick();
        var behaviour = evalString(getSchema().getString("Behaviour"), "");
        var animation = getAnimation();
        if (animation != null && (getTime() == animation.getDuration() - 1 || animation.getDuration() == 1) && !string.IsNullOrWhiteSpace(behaviour))
        {
            try
            {
                getMascot().setBehavior(Main.getInstance().getConfiguration(getMascot().imageSet).buildBehavior(behaviour, getMascot()));
            }
            catch (System.Exception e)
            {
                Log.Severe("Interact failed", e);
                Main.showError(Main.getInstance().getLanguageBundle().getString("FailedSetBehaviourErrorMessage"), e);
            }
        }
    }
}

public class Regist : ActionBase
{
    private double scaling;
    public Regist(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }

    public override void init(Mascot mascot)
    {
        base.init(mascot);
        scaling = double.Parse(Main.getInstance().getProperties().getProperty("Scaling", "1.0"), System.Globalization.CultureInfo.InvariantCulture);
    }

    public override bool hasNext()
    {
        var offsetX = (int)Math.Round(evalInt(getSchema().getString("OffsetX"), 0) * scaling);
        if (evalString(getSchema().getString("OffsetType"), "ImageAnchor") == getSchema().getString("Origin") && getMascot().getImage() != null)
            offsetX = 0 - offsetX + getMascot().getImage()!.getCenter().X;
        var notMoved = Math.Abs(getEnvironment().getCursor().getX() - getMascot().anchor.x + offsetX) < 5;
        return base.hasNext() && notMoved;
    }

    protected override void tick()
    {
        getMascot().dragging = true;
        getAnimation()?.next(getMascot(), getTime());
        if (getAnimation() != null && getTime() + 1 >= getAnimation()!.getDuration())
        {
            getMascot().lookRight = Random.Shared.NextDouble() < 0.5;
            throw new LostGroundException();
        }
    }
}

public class Broadcast : Animate
{
    public Broadcast(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }
}

public class BroadcastStay : Stay
{
    public BroadcastStay(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }
}

public class BroadcastMove : Move
{
    public BroadcastMove(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }
}

public class BroadcastJump : Jump
{
    public BroadcastJump(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }
}

public class ScanMove : Move
{
    public ScanMove(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }
}

public class ScanJump : Jump
{
    public ScanJump(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }
}

public class ScanInteract : Interact
{
    public ScanInteract(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }
}
