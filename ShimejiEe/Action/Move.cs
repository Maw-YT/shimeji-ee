using GroupFinity.Mascot.Animation;
using GroupFinity.Mascot.Exception;
using GroupFinity.Mascot.I18n;
using GroupFinity.Mascot.Script;

namespace GroupFinity.Mascot.Action;

public class Move : BorderedAction
{
    private const string PARAMETER_TARGETX = "TargetX";
    private const int DEFAULT_TARGETX = int.MaxValue;
    private const string PARAMETER_TARGETY = "TargetY";
    private const int DEFAULT_TARGETY = int.MaxValue;

    protected bool turning;
    private bool? hasTurning;

    public Move(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }

    public override bool hasNext()
    {
        var targetX = getTargetX();
        var targetY = getTargetY();
        var hasNotReached = (targetX != int.MinValue && getMascot().anchor.x == targetX) ||
                            (targetY != int.MinValue && getMascot().anchor.y == targetY);
        return base.hasNext() && (!hasNotReached || turning);
    }

    protected override void tick()
    {
        base.tick();
        if (getBorder() != null && !getBorder()!.isOn(getMascot().anchor))
        {
            Log.Info($"Lost Ground ({getMascot()},{this})");
            throw new LostGroundException();
        }

        var targetX = getTargetX();
        var targetY = getTargetY();
        var down = false;

        if (targetX != DEFAULT_TARGETX && getMascot().anchor.x != targetX)
        {
            turning = hasTurningAnimation() && (turning || getMascot().anchor.x < targetX != getMascot().lookRight);
            getMascot().lookRight = getMascot().anchor.x < targetX;
        }
        if (targetY != DEFAULT_TARGETY)
            down = getMascot().anchor.y < targetY;

        var animation = getAnimation();
        if (animation == null)
            throw new LostGroundException();
        if (turning && getTime() >= animation.getDuration())
            turning = false;

        animation.next(getMascot(), getTime());

        if (targetX != DEFAULT_TARGETX)
        {
            if ((getMascot().lookRight && getMascot().anchor.x >= targetX) ||
                (!getMascot().lookRight && getMascot().anchor.x <= targetX))
                getMascot().anchor = new ScriptPoint(targetX, getMascot().anchor.y);
        }
        if (targetY != DEFAULT_TARGETY)
        {
            if ((down && getMascot().anchor.y >= targetY) ||
                (!down && getMascot().anchor.y <= targetY))
                getMascot().anchor = new ScriptPoint(getMascot().anchor.x, targetY);
        }
    }

    protected override Animation.Animation? getAnimation()
    {
        foreach (var animation in getAnimations())
        {
            if (animation.isEffective(getVariables()) && turning == animation.isTurn())
                return animation;
        }
        return null;
    }

    protected bool hasTurningAnimation()
    {
        if (hasTurning == null)
        {
            hasTurning = false;
            foreach (var animation in getAnimations())
            {
                if (animation.isTurn())
                {
                    hasTurning = true;
                    break;
                }
            }
        }
        return hasTurning.Value;
    }

    protected bool isTurning() => turning;
    private int getTargetX() => evalInt(getSchema().getString(PARAMETER_TARGETX), DEFAULT_TARGETX);
    private int getTargetY() => evalInt(getSchema().getString(PARAMETER_TARGETY), DEFAULT_TARGETY);
}
