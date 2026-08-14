namespace GroupFinity.Mascot.Environment;

public interface Border
{
    bool isOn(ScriptPoint location);
    ScriptPoint move(ScriptPoint location);
}
