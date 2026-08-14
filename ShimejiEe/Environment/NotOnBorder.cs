namespace GroupFinity.Mascot.Environment;

public sealed class NotOnBorder : Border
{
    public static readonly NotOnBorder INSTANCE = new();

    private NotOnBorder() { }

    public bool isOn(ScriptPoint location) => false;

    public ScriptPoint move(ScriptPoint location) => location;
}
