namespace GroupFinity.Mascot.Environment;

public sealed class FloorCeiling : Border
{
    private readonly Area area;
    private readonly bool bottom;

    public FloorCeiling(Area area, bool bottom)
    {
        this.area = area;
        this.bottom = bottom;
    }

    public Area getArea() => area;
    public bool isBottom() => bottom;
    public int getY() => bottom ? area.getBottom() : area.getTop();
    public int y => getY();
    public int getLeft() => area.getLeft();
    public int getRight() => area.getRight();
    public int getDY() => bottom ? area.getDbottom() : area.getDtop();
    public int getDLeft() => area.getDleft();
    public int getDRight() => area.getDright();
    public int getWidth() => area.getWidth();

    public bool isOn(ScriptPoint location)
        => area.isVisible() && getY() == location.y && getLeft() <= location.x && location.x <= getRight();

    public ScriptPoint move(ScriptPoint location)
    {
        if (!area.isVisible())
            return location;

        var d = getRight() - getDRight() - (getLeft() - getDLeft());
        if (d == 0)
            return location;

        var newLocation = new ScriptPoint(
            (location.x - (getLeft() - getDLeft())) * ((getRight() - getLeft()) / d) + getLeft(),
            location.y + getDY());

        if (Math.Abs(newLocation.x - location.x) >= 80 || newLocation.y - location.y > 20 || newLocation.y - location.y < -80)
            return location;
        return newLocation;
    }
}
