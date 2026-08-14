using GroupFinity.Mascot.Action;
using GroupFinity.Mascot.Config;
using GroupFinity.Mascot.Environment;
using GroupFinity.Mascot.Exception;
using GroupFinity.Mascot.Hotspot;

namespace GroupFinity.Mascot.Behavior;

public interface Behavior
{
    void init(Mascot mascot);
    void mousePressed(MouseEventArgs e);
    void mouseReleased(MouseEventArgs e);
    void next();
    string getName();
}

public sealed class UserBehavior : Behavior
{
    public const string BEHAVIOURNAME_FALL = "Fall";
    public const string BEHAVIOURNAME_DRAGGED = "Dragged";
    public const string BEHAVIOURNAME_THROWN = "Thrown";

    private enum HotspotResult { INACTIVE, ACTIVE_NULL, ACTIVE }

    private readonly string name;
    private readonly Configuration configuration;
    private readonly Action.Action action;
    private Mascot? mascot;

    public UserBehavior(string name, Action.Action action, Configuration configuration)
    {
        this.name = name;
        this.action = action;
        this.configuration = configuration;
    }

    public override string ToString() => "Behavior(" + name + ")";
    public string getName() => name;

    public void init(Mascot mascot)
    {
        this.mascot = mascot;
        action.init(mascot);
        if (!action.hasNext())
            mascot.setBehavior(configuration.buildNextBehavior(name, mascot));
    }

    public void mousePressed(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        var handled = false;
        if (mascot!.getHotspots().Count > 0)
        {
            foreach (var hotspot in mascot.getHotspots())
            {
                if (hotspot.contains(mascot, e.Location) &&
                    Main.getInstance().getConfiguration(mascot.imageSet).isBehaviorEnabled(hotspot.getBehaviour(), mascot))
                {
                    handled = true;
                    mascot.setCursorPosition(e.Location);
                    if (hotspot.getBehaviour() != null)
                        mascot.setBehavior(configuration.buildBehavior(hotspot.getBehaviour(), mascot));
                    break;
                }
            }
        }
        if (!handled && action is ActionBase ab)
            handled = !ab.isDraggable();
        if (!handled)
            mascot.setBehavior(configuration.buildBehavior(configuration.getSchema().getString(BEHAVIOURNAME_DRAGGED)));
    }

    public void mouseReleased(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        if (mascot!.isHotspotClicked())
            mascot.setCursorPosition(null);
        if (mascot.dragging)
        {
            mascot.dragging = false;
            mascot.setBehavior(configuration.buildBehavior(configuration.getSchema().getString(BEHAVIOURNAME_THROWN)));
        }
    }

    public void next()
    {
        try
        {
            if (action.hasNext())
                action.next();

            var hotspotIsActive = HotspotResult.INACTIVE;
            if (mascot!.isHotspotClicked())
            {
                foreach (var hotspot in mascot.getHotspots())
                {
                    if (mascot.getCursorPosition() is Point cursorPoint && hotspot.contains(mascot, cursorPoint))
                    {
                        hotspotIsActive = HotspotResult.ACTIVE_NULL;
                        if (hotspot.getBehaviour() != null)
                        {
                            hotspotIsActive = HotspotResult.ACTIVE;
                            mascot.setBehavior(configuration.buildBehavior(hotspot.getBehaviour(), mascot));
                        }
                        break;
                    }
                }
                if (hotspotIsActive == HotspotResult.INACTIVE)
                    mascot.setCursorPosition(null);
            }

            if (hotspotIsActive != HotspotResult.ACTIVE)
            {
                if (action.hasNext())
                {
                    var bounds = mascot.getBounds();
                    if (bounds.X + bounds.Width <= getEnvironment().getScreen().getLeft() ||
                        getEnvironment().getScreen().getRight() <= bounds.X ||
                        getEnvironment().getScreen().getBottom() <= bounds.Y)
                    {
                        var area = bool.Parse(Main.getInstance().getProperties().getProperty("Multiscreen", "true"))
                            ? getEnvironment().getScreen() : getEnvironment().getWorkArea();
                        mascot.anchor = new ScriptPoint((int)(Random.Shared.NextDouble() * (area.getRight() - area.getLeft())) + area.getLeft(), area.getTop() - 256);
                        mascot.setBehavior(configuration.buildBehavior(configuration.getSchema().getString(BEHAVIOURNAME_FALL)));
                    }
                }
                else
                    mascot.setBehavior(configuration.buildNextBehavior(name, mascot));
            }
        }
        catch (LostGroundException)
        {
            mascot!.setCursorPosition(null);
            mascot.dragging = false;
            mascot.setBehavior(configuration.buildBehavior(configuration.getSchema().getString(BEHAVIOURNAME_FALL)));
        }
    }

    private MascotEnvironment getEnvironment() => mascot!.environment;
}
