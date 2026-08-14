namespace GroupFinity.Mascot.Environment;

public sealed class Wall : Border
{
    private readonly Area area;
    private readonly bool right;

    public Wall(Area area, bool right)
    {
        this.area = area;
        this.right = right;
    }

    public Area getArea() => area;
    public bool isRight() => right;
    public int getX() => right ? area.getRight() : area.getLeft();
    public int x => getX();
    public int getTop() => area.getTop();
    public int getBottom() => area.getBottom();
    public int getDX() => right ? area.getDright() : area.getDleft();
    public int getDTop() => area.getDtop();
    public int getDBottom() => area.getDbottom();
    public int getHeight() => area.getHeight();

    public bool isOn(ScriptPoint location)
        => area.isVisible() && getX() == location.x && getTop() <= location.y && location.y <= getBottom();

    public ScriptPoint move(ScriptPoint location)
    {
        if (!area.isVisible())
            return location;

        var d = getBottom() - getDBottom() - (getTop() - getDTop());
        if (d == 0)
            return location;

        var newLocation = new ScriptPoint(location.x + getDX(),
            (location.y - (getTop() - getDTop())) * (getBottom() - getTop()) / d + getTop());

        if (Math.Abs(newLocation.x - location.x) >= 80 || Math.Abs(newLocation.y - location.y) >= 80)
            return location;
        return newLocation;
    }
}
