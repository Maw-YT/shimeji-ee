using System.Drawing;
using System.Runtime.InteropServices;
using GroupFinity.Mascot.Win;

namespace GroupFinity.Mascot.Environment;

public abstract class Environment
{
    protected static Rectangle screenRect = new(0, 0, 1920, 1080);

    protected static Dictionary<string, Rectangle> screenRects = new();

    private static readonly Thread ScreenThread = new(UpdateLoop) { IsBackground = true, Priority = ThreadPriority.Lowest };
    private static readonly NativeMethods.MonitorEnumProc MonitorCallback = OnMonitor;
    private static readonly object ScreenSync = new();
    private static readonly Dictionary<string, Rectangle> PendingMonitors = new();
    private static Rectangle pendingVirtual;
    private static bool _started;

    public ComplexArea complexScreen { get; } = new();
    public Area screen { get; } = new();
    public Location cursor { get; } = new();

    public abstract Area getWorkArea();
    public abstract Area getActiveIE();
    public abstract string getActiveIETitle();
    public abstract void moveActiveIE(ScriptPoint point);
    public abstract void restoreIE();
    public abstract void refreshCache();
    public abstract void dispose();

    public Area WorkArea => getWorkArea();

    public void init()
    {
        if (!_started)
        {
            _started = true;
            updateScreenRect();
            ScreenThread.Start();
        }
        tick();
    }

    public virtual void tick()
    {
        screen.set(getScreenRect());
        complexScreen.set(new Dictionary<string, Rectangle>(screenRects));
        cursor.set(getCursorPos());
    }

    public Area getScreen() => screen;
    public ICollection<Area> getScreens() => complexScreen.getAreas();
    public ComplexArea getComplexScreen() => complexScreen;
    public Location getCursor() => cursor;

    protected static Rectangle getScreenRect() => screenRect;

    private static ScriptPoint getCursorPos()
    {
        if (NativeMethods.GetCursorPos(out var p))
            return new ScriptPoint(p.x, p.y);
        return new ScriptPoint(0, 0);
    }

    private static void UpdateLoop()
    {
        try
        {
            while (true)
            {
                updateScreenRect();
                Thread.Sleep(5000);
            }
        }
        catch (ThreadInterruptedException) { }
    }

    private static bool OnMonitor(IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.RECT lprcMonitor, IntPtr dwData)
    {
        var info = new NativeMethods.MONITORINFOEX
        {
            cbSize = Marshal.SizeOf<NativeMethods.MONITORINFOEX>()
        };
        if (!NativeMethods.GetMonitorInfo(hMonitor, ref info) || string.IsNullOrEmpty(info.szDevice))
            return true;
        var r = new Rectangle(info.rcMonitor.left, info.rcMonitor.top, info.rcMonitor.Width, info.rcMonitor.Height);
        PendingMonitors[info.szDevice] = r;
        pendingVirtual = pendingVirtual.IsEmpty ? r : Rectangle.Union(pendingVirtual, r);
        return true;
    }

    private static void updateScreenRect()
    {
        lock (ScreenSync)
        {
            PendingMonitors.Clear();
            pendingVirtual = Rectangle.Empty;
            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorCallback, IntPtr.Zero);
            GC.KeepAlive(MonitorCallback);
            if (PendingMonitors.Count == 0)
                return;
            screenRects = new Dictionary<string, Rectangle>(PendingMonitors);
            screenRect = pendingVirtual;
        }
    }

    public bool isScreenTopBottom(ScriptPoint location)
    {
        var count = 0;
        foreach (var area in getScreens())
        {
            if (area.getTopBorder().isOn(location)) ++count;
            if (area.getBottomBorder().isOn(location)) ++count;
        }
        if (count == 0)
        {
            if (getWorkArea().getTopBorder().isOn(location)) return true;
            if (getWorkArea().getBottomBorder().isOn(location)) return true;
        }
        return count == 1;
    }

    public bool isScreenLeftRight(ScriptPoint location)
    {
        var count = 0;
        foreach (var area in getScreens())
        {
            if (area.getLeftBorder().isOn(location)) ++count;
            if (area.getRightBorder().isOn(location)) ++count;
        }
        if (count == 0)
        {
            if (getWorkArea().getLeftBorder().isOn(location)) return true;
            if (getWorkArea().getRightBorder().isOn(location)) return true;
        }
        return count == 1;
    }
}
