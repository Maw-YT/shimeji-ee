namespace GroupFinity.Mascot;

/// <summary>
/// Java-compatible point with public x/y fields for Nashorn-style scripts.
/// </summary>
public sealed class ScriptPoint
{
    public int x;
    public int y;

    public ScriptPoint() { }

    public ScriptPoint(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public ScriptPoint(ScriptPoint other)
    {
        x = other.x;
        y = other.y;
    }

    public bool Equals(ScriptPoint? other) => other != null && x == other.x && y == other.y;

    public override bool Equals(object? obj) => obj is ScriptPoint p && Equals(p);

    public override int GetHashCode() => HashCode.Combine(x, y);

    public override string ToString() => $"Point[{x},{y}]";
}
