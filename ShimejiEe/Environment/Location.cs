namespace GroupFinity.Mascot.Environment;

public sealed class Location
{
    public int x { get; set; }
    public int y { get; set; }
    public int dx { get; set; }
    public int dy { get; set; }

    public int getX() => x;
    public int getY() => y;
    public int getDx() => dx;
    public int getDy() => dy;

    public void set(ScriptPoint value)
    {
        dx = (dx + value.x - x) / 2;
        dy = (dy + value.y - y) / 2;
        x = value.x;
        y = value.y;
    }

    public void set(System.Drawing.Point value) => set(new ScriptPoint(value.X, value.Y));
}
