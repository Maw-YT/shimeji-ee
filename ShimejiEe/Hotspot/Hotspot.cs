using System.Drawing;
using GroupFinity.Mascot;

namespace GroupFinity.Mascot.Hotspot;

public sealed class Hotspot
{
    private readonly string behaviour;
    private readonly RectangleF shape;
    private readonly bool ellipse;

    public Hotspot(string behaviour, RectangleF shape, bool ellipse = false)
    {
        this.behaviour = behaviour;
        this.shape = shape;
        this.ellipse = ellipse;
    }

    public bool contains(Mascot mascot, Point point)
    {
        if (mascot.lookRight)
            point = new Point(mascot.getBounds().Width - point.X, point.Y);
        if (ellipse)
        {
            var rx = shape.Width / 2f;
            var ry = shape.Height / 2f;
            if (rx <= 0 || ry <= 0) return false;
            var dx = (point.X - (shape.X + rx)) / rx;
            var dy = (point.Y - (shape.Y + ry)) / ry;
            return dx * dx + dy * dy <= 1;
        }
        return shape.Contains(point);
    }

    public string getBehaviour() => behaviour;
}
