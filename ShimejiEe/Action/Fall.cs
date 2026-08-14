using GroupFinity.Mascot.Animation;
using GroupFinity.Mascot.I18n;
using GroupFinity.Mascot.Script;

namespace GroupFinity.Mascot.Action;

public class Fall : ActionBase
{
    public const string PARAMETER_INITIALVX = "InitialVX";
    private const int DEFAULT_INITIALVX = 0;
    private const string PARAMETER_INITIALVY = "InitialVY";
    private const int DEFAULT_INITIALVY = 0;
    public const string PARAMETER_RESISTANCEX = "ResistanceX";
    private const double DEFAULT_RESISTANCEX = 0.05;
    public const string PARAMETER_RESISTANCEY = "ResistanceY";
    private const double DEFAULT_RESISTANCEY = 0.1;
    public const string PARAMETER_GRAVITY = "Gravity";
    private const double DEFAULT_GRAVITY = 2;
    public const string VARIABLE_VELOCITYX = "VelocityX";
    public const string VARIABLE_VELOCITYY = "VelocityY";

    private double velocityX, velocityY, modX, modY;

    public Fall(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }

    public override void init(Mascot mascot)
    {
        base.init(mascot);
        velocityX = getInitialVx();
        velocityY = getInitialVy();
    }

    public override bool hasNext()
    {
        var pos = getMascot().anchor;
        var onBorder = getEnvironment().getFloor().isOn(pos) || getEnvironment().getWall().isOn(pos);
        return base.hasNext() && !onBorder;
    }

    protected override void tick()
    {
        if (velocityX != 0)
            getMascot().lookRight = velocityX > 0;

        velocityX -= velocityX * getResistanceX();
        velocityY = velocityY - (velocityY * getResistanceY()) + getGravity();

        putVariable(getSchema().getString(VARIABLE_VELOCITYX), velocityX);
        putVariable(getSchema().getString(VARIABLE_VELOCITYY), velocityY);

        modX += double.IsFinite(velocityX) ? velocityX % 1 : 0;
        modY += double.IsFinite(velocityY) ? velocityY % 1 : 0;
        if (!double.IsFinite(velocityX)) velocityX = 0;
        if (!double.IsFinite(velocityY)) velocityY = 0;
        velocityX = Math.Clamp(velocityX, -400, 400);
        velocityY = Math.Clamp(velocityY, -400, 400);
        var dx = (int)velocityX + (int)modX;
        var dy = (int)velocityY + (int)modY;
        modX %= 1;
        modY %= 1;

        var dev = Math.Max(1, Math.Max(Math.Abs(dx), Math.Abs(dy)));
        var start = getMascot().anchor;

        for (var i = 0; i <= dev; ++i)
        {
            var x = start.x + dx * i / dev;
            var y = start.y + dy * i / dev;
            getMascot().anchor = new ScriptPoint(x, y);
            if (dy > 0)
            {
                var landed = false;
                for (var j = -80; j <= 0; ++j)
                {
                    getMascot().anchor = new ScriptPoint(x, y + j);
                    if (getEnvironment().getFloor(true).isOn(getMascot().anchor))
                    {
                        landed = true;
                        break;
                    }
                }
                if (landed) break;
            }
            if (getEnvironment().getWall(true).isOn(getMascot().anchor))
                break;
        }

        getAnimation()?.next(getMascot(), getTime());
    }

    private int getInitialVx() => evalInt(getSchema().getString(PARAMETER_INITIALVX), DEFAULT_INITIALVX);
    private int getInitialVy() => evalInt(getSchema().getString(PARAMETER_INITIALVY), DEFAULT_INITIALVY);
    private double getGravity() => evalDouble(getSchema().getString(PARAMETER_GRAVITY), DEFAULT_GRAVITY);
    private double getResistanceX() => evalDouble(getSchema().getString(PARAMETER_RESISTANCEX), DEFAULT_RESISTANCEX);
    private double getResistanceY() => evalDouble(getSchema().getString(PARAMETER_RESISTANCEY), DEFAULT_RESISTANCEY);
}
