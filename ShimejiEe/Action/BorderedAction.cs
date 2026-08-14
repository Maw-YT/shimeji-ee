using GroupFinity.Mascot.Animation;
using GroupFinity.Mascot.Environment;
using GroupFinity.Mascot.I18n;
using GroupFinity.Mascot.Script;

namespace GroupFinity.Mascot.Action;

public abstract class BorderedAction : ActionBase
{
    private const string PARAMETER_BORDERTYPE = "BorderType";
    public const string BORDERTYPE_CEILING = "Ceiling";
    public const string BORDERTYPE_WALL = "Wall";
    public const string BORDERTYPE_FLOOR = "Floor";

    private Border? border;

    protected BorderedAction(PropertiesBundle schema, List<Animation.Animation> animations, VariableMap context)
        : base(schema, animations, context) { }

    public override void init(Mascot mascot)
    {
        base.init(mascot);
        var borderType = evalString(getSchema().getString(PARAMETER_BORDERTYPE), null!);
        if (getSchema().getString(BORDERTYPE_CEILING).Equals(borderType))
            border = getEnvironment().getCeiling();
        else if (getSchema().getString(BORDERTYPE_WALL).Equals(borderType))
            border = getEnvironment().getWall();
        else if (getSchema().getString(BORDERTYPE_FLOOR).Equals(borderType))
            border = getEnvironment().getFloor();
    }

    protected override void tick()
    {
        if (border != null)
            getMascot().anchor = border.move(getMascot().anchor);
    }

    protected Border? getBorder() => border;
}
