using GroupFinity.Mascot.Behavior;
using GroupFinity.Mascot.Environment;
using GroupFinity.Mascot.Exception;
using GroupFinity.Mascot.Hotspot;
using GroupFinity.Mascot.Image;
using GroupFinity.Mascot.Script;
using GroupFinity.Mascot.Sound;

namespace GroupFinity.Mascot;

public sealed class Mascot
{
    private static int lastId;
    private readonly int id;
    public string imageSet { get; set; }
    private readonly TranslucentWindow window = NativeFactory.getInstance().newTransparentWindow();
    private Manager? manager;
    public ScriptPoint anchor { get; set; } = new();
    private MascotImage? image;
    public bool lookRight { get; set; }
    private Behavior.Behavior? behavior;
    private int time;
    private bool animating = true;
    public bool paused { get; set; }
    public bool dragging { get; set; }
    public MascotEnvironment environment { get; }
    private string? sound;
    private readonly List<string> affordances = new();
    private readonly List<Hotspot.Hotspot> hotspots = new();
    private Point? cursor;
    private VariableMap? variables;
    private int uiUpdateQueued;
    private MouseEventArgs? queuedPress;
    private MouseEventArgs? queuedRelease;
    private volatile string? pendingBehavior;

    public Mascot(string imageSet)
    {
        id = Interlocked.Increment(ref lastId);
        this.imageSet = imageSet;
        environment = new MascotEnvironment(this);
        UiSync.Send(() =>
        {
            window.MouseDown += (_, e) => queuePress(e);
            window.MouseUp += (_, e) =>
            {
                if (e.Button == MouseButtons.Right)
                    showPopup(e.X, e.Y);
                else
                    queueRelease(e);
            };
            window.MouseMove += (_, e) =>
            {
                if (paused) refreshCursor(false);
                else if (isHotspotClicked()) setCursorPosition(e.Location);
                else refreshCursor(e.Location);
            };
        });
    }

    public override string ToString() => "mascot" + id;
    public int Id => id;

    private Ui.SpeechBubble? bubble;

    private void queuePress(MouseEventArgs e) => queuedPress = e;
    private void queueRelease(MouseEventArgs e) => queuedRelease = e;

    private void mousePressed(MouseEventArgs e)
    {
        if (!paused && behavior != null)
        {
            try { behavior.mousePressed(e); }
            catch (System.Exception ex)
            {
                Log.Severe("Fatal Error", ex);
                if (ex is CantBeAliveException)
                {
                    Main.showError(Main.getInstance().getLanguageBundle().getString("SevereShimejiErrorErrorMessage"), ex);
                    dispose();
                }
            }
        }
    }

    private void mouseReleased(MouseEventArgs e)
    {
        if (paused || behavior == null) return;
        try { behavior.mouseReleased(e); }
        catch (System.Exception ex)
        {
            Log.Severe("Fatal Error", ex);
            dragging = false;
            try { setBehavior(Main.getInstance().getConfiguration(imageSet).buildBehavior("Fall")); }
            catch { /* keep current behavior */ }
        }
    }

    private void showPopup(int x, int y)
    {
        var language = Main.getInstance().getLanguageBundle();
        var menu = new ContextMenuStrip();
        menu.Opening += (_, _) => animating = false;
        menu.Closed += (_, _) => animating = true;

        menu.Items.Add(language.getString("CallAnother"), null, (_, _) => Main.getInstance().createMascot(imageSet));
        menu.Items.Add(language.getString("Respond"), null, (_, _) => Ai.AiCompanion.PromptUser(this));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(language.getString("FollowCursor"), null, (_, _) => manager?.setBehaviorAll(Main.getInstance().getConfiguration(imageSet), Main.BEHAVIOR_GATHER, imageSet));
        menu.Items.Add(language.getString("RestoreWindows"), null, (_, _) => NativeFactory.getInstance().getEnvironment().restoreIE());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(paused ? language.getString("ResumeAnimations") : language.getString("PauseAnimations"), null, (_, _) => paused = !paused);
        var debugToggle = new ToolStripMenuItem(language.getString("ActionDebug")) { CheckOnClick = true, Checked = Ui.ActionDebug.Enabled };
        debugToggle.CheckedChanged += (_, _) => Ui.ActionDebug.Enabled = debugToggle.Checked;
        menu.Items.Add(debugToggle);
        if (Ui.ActionDebug.Enabled)
        {
            var force = new ToolStripMenuItem(language.getString("SetBehaviour"));
            force.DropDownOpening += (_, _) => Ui.ActionDebug.FillForceMenu(force, this);
            menu.Items.Add(force);
        }
        var allowed = new ToolStripMenuItem(language.getString("AllowedBehaviours"));
        allowed.DropDownOpening += (_, _) => Ui.ActionDebug.FillAllowedMenu(allowed, this);
        menu.Items.Add(allowed);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(language.getString("Dismiss"), null, (_, _) => dispose());
        menu.Items.Add(language.getString("DismissOthers"), null, (_, _) => manager?.remainOne(imageSet));
        menu.Items.Add(language.getString("DismissAllOthers"), null, (_, _) => manager?.remainOne(this));
        menu.Items.Add(language.getString("DismissAll"), null, (_, _) => Main.getInstance().exit());
        menu.Show(window.ScreenLocation.X + x, window.ScreenLocation.Y + y);
    }

    public void tick()
    {
        var press = Interlocked.Exchange(ref queuedPress, null);
        if (press != null) mousePressed(press);
        var release = Interlocked.Exchange(ref queuedRelease, null);
        if (release != null) mouseReleased(release);

        var pending = Interlocked.Exchange(ref pendingBehavior, null);
        if (pending != null)
        {
            try
            {
                dragging = false;
                var configuration = Main.getInstance().getConfiguration(imageSet);
                if (!configuration.getBehaviorNames().Contains(pending))
                    throw new InvalidOperationException(pending);
                setBehavior(configuration.buildBehavior(pending));
            }
            catch (System.Exception e)
            {
                Log.Severe("Could not force behaviour " + pending, e);
                Main.showError(Main.getInstance().getLanguageBundle().getString("CouldNotSetBehaviourErrorMessage"), e);
            }
        }

        if (!(animating && !paused) || behavior == null) return;
        try { behavior.next(); }
        catch (CantBeAliveException e)
        {
            Log.Severe("Fatal Error", e);
            Main.showError(Main.getInstance().getLanguageBundle().getString("CouldNotGetNextBehaviourErrorMessage"), e);
            dispose();
        }
        catch (System.Exception e)
        {
            Log.Severe("Mascot update failed", e);
            try
            {
                dragging = false;
                setBehavior(Main.getInstance().getConfiguration(imageSet).buildBehavior("Fall"));
            }
            catch (System.Exception fallback)
            {
                Log.Severe("Failed to recover mascot", fallback);
            }
        }
        time++;
    }

    public void apply()
    {
        var snapshot = image;
        var bounds = getBounds();
        var shouldPresent = animating && !paused;
        if (Interlocked.CompareExchange(ref uiUpdateQueued, 1, 0) != 0)
            return;
        UiSync.Post(() =>
        {
            try
            {
                if (shouldPresent)
                {
                    if (snapshot != null)
                        window.present(snapshot.getImage(), bounds.X, bounds.Y);
                    else
                        window.hide();
                }
                bubble?.Follow(bounds);
            }
            finally
            {
                Interlocked.Exchange(ref uiUpdateQueued, 0);
            }
        });

        if (!Sounds.isMuted() && sound != null && Sounds.contains(sound))
        {
            var clip = Sounds.getSound(sound);
            if (clip != null && !clip.IsRunning)
                clip.Play();
        }
    }

    public void say(string text)
    {
        UiSync.Post(() =>
        {
            bubble ??= new Ui.SpeechBubble();
            bubble.ShowText(text);
            bubble.Follow(getBounds());
        });
    }

    public void hideSpeech()
    {
        UiSync.Post(() => bubble?.Hide());
    }

    public void dispose()
    {
        animating = false;
        UiSync.Post(() =>
        {
            bubble?.Dispose();
            bubble = null;
        });
        window.dispose();
        affordances.Clear();
        manager?.remove(this);
    }

    private void refreshCursor(Point position)
    {
        var useHand = false;
        foreach (var hotspot in hotspots)
        {
            if (hotspot.contains(this, position) &&
                Main.getInstance().getConfiguration(imageSet).isBehaviorEnabled(hotspot.getBehaviour(), this))
            {
                useHand = true;
                break;
            }
        }
        refreshCursor(useHand);
    }

    private void refreshCursor(bool useHand) => window.setHandCursor(useHand);

    public void queueBehavior(string name) => pendingBehavior = name;
    public Manager? getManager() => manager;
    public void setManager(Manager? value) => manager = value;
    public MascotImage? getImage() => image;
    public void setImage(MascotImage? value) => image = value;
    public Rectangle getBounds()
    {
        if (image != null)
            return new Rectangle(anchor.x - image.getCenter().X, anchor.y - image.getCenter().Y, image.getSize().Width, image.getSize().Height);
        return Rectangle.Empty;
    }
    public int getTime() => time;
    public Behavior.Behavior? getBehavior() => behavior;
    public void setBehavior(Behavior.Behavior value)
    {
        behavior = value;
        behavior.init(this);
    }
    public int count => manager?.getCount(imageSet) ?? 0;
    public int totalCount => manager?.getCount() ?? 0;
    public List<string> getAffordances() => affordances;
    public List<Hotspot.Hotspot> getHotspots() => hotspots;
    public string? getSound() => sound;
    public void setSound(string? name) => sound = name;
    public bool isHotspotClicked() => cursor != null;
    public Point? getCursorPosition() => cursor;
    public void setCursorPosition(Point? point)
    {
        cursor = point;
        if (point == null) refreshCursor(false);
        else refreshCursor(point.Value);
    }
    public VariableMap getVariables() => variables ??= new VariableMap();
}
