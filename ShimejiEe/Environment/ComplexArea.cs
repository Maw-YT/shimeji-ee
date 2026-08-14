using System.Drawing;

namespace GroupFinity.Mascot.Environment;

public sealed class ComplexArea
{
    private readonly Dictionary<string, Area> areas = new();

    public void set(Dictionary<string, Rectangle> rectangles)
    {
        retain(rectangles.Keys);
        foreach (var e in rectangles)
            set(e.Key, e.Value);
    }

    public void set(string name, Rectangle value)
    {
        foreach (var area in areas.Values)
        {
            if (area.getLeft() == value.X && area.getTop() == value.Y &&
                area.getWidth() == value.Width && area.getHeight() == value.Height)
                return;
        }

        if (!areas.TryGetValue(name, out var target))
        {
            target = new Area();
            areas[name] = target;
        }
        target.set(value);
    }

    public void retain(IEnumerable<string> deviceNames)
    {
        var keep = new HashSet<string>(deviceNames);
        foreach (var key in areas.Keys.ToList())
        {
            if (!keep.Contains(key))
                areas.Remove(key);
        }
    }

    public FloorCeiling? getBottomBorder(ScriptPoint location)
    {
        FloorCeiling? ret = null;
        foreach (var area in areas.Values)
        {
            if (area.getBottomBorder().isOn(location))
                ret = area.getBottomBorder();
        }
        foreach (var area in areas.Values)
        {
            if (area.getTopBorder().isOn(location))
                ret = null;
        }
        return ret;
    }

    public FloorCeiling? getTopBorder(ScriptPoint location)
    {
        FloorCeiling? ret = null;
        foreach (var area in areas.Values)
        {
            if (area.getTopBorder().isOn(location))
                ret = area.getTopBorder();
        }
        foreach (var area in areas.Values)
        {
            if (area.getBottomBorder().isOn(location))
                ret = null;
        }
        return ret;
    }

    public Wall? getLeftBorder(ScriptPoint location)
    {
        Wall? ret = null;
        foreach (var area in areas.Values)
        {
            if (area.getLeftBorder().isOn(location))
                ret = area.getRightBorder();
        }
        foreach (var area in areas.Values)
        {
            if (area.getRightBorder().isOn(location))
                ret = null;
        }
        return ret;
    }

    public Wall? getRightBorder(ScriptPoint location)
    {
        Wall? ret = null;
        foreach (var area in areas.Values)
        {
            if (area.getRightBorder().isOn(location))
                ret = area.getRightBorder();
        }
        foreach (var area in areas.Values)
        {
            if (area.getLeftBorder().isOn(location))
                ret = null;
        }
        return ret;
    }

    public ICollection<Area> getAreas() => areas.Values;
}
