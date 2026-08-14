using System.Drawing;
using System.Text;
using GroupFinity.Mascot.Environment;

namespace GroupFinity.Mascot.Win;

internal sealed class WindowsEnvironment : Environment.Environment
{
    private static readonly Dictionary<IntPtr, bool> ieCache = new();
    public static readonly Area workArea = new();
    public static readonly Area activeIE = new();
    private static IntPtr activeIEobject = IntPtr.Zero;
    private static string[]? windowTitles;
    private static string[]? windowTitlesBlacklist;
    private static long lastCacheClear;

    private enum IEResult { INVALID, NOT_IE, IE_OUT_OF_BOUNDS, IE }

    private static bool isIE(IntPtr ie)
    {
        if (ieCache.TryGetValue(ie, out var cached))
            return cached;

        var title = GetTitle(ie);
        if (string.IsNullOrEmpty(title) || title == "Program Manager")
        {
            ieCache[ie] = false;
            return false;
        }

        var blacklistInUse = false;
        windowTitlesBlacklist ??= Main.getInstance().getProperties().getProperty("InteractiveWindowsBlacklist", "").Split('/');
        foreach (var windowTitle in windowTitlesBlacklist)
        {
            if (!string.IsNullOrWhiteSpace(windowTitle))
            {
                blacklistInUse = true;
                if (title.Contains(windowTitle))
                {
                    ieCache[ie] = false;
                    return false;
                }
            }
        }

        var whitelistInUse = false;
        windowTitles ??= Main.getInstance().getProperties().getProperty("InteractiveWindows", "").Split('/');
        foreach (var windowTitle in windowTitles)
        {
            if (!string.IsNullOrWhiteSpace(windowTitle))
            {
                whitelistInUse = true;
                if (title.Contains(windowTitle))
                {
                    ieCache[ie] = true;
                    return true;
                }
            }
        }

        ieCache[ie] = !(whitelistInUse || !blacklistInUse);
        return ieCache[ie];
    }

    private static string GetTitle(IntPtr ie)
    {
        var sb = new StringBuilder(1024);
        NativeMethods.GetWindowText(ie, sb, 1024);
        return sb.ToString();
    }

    private static IEResult isViableIE(IntPtr ie)
    {
        if (NativeMethods.IsWindowVisible(ie))
        {
            try
            {
                var hr = NativeMethods.DwmGetWindowAttribute(ie, NativeMethods.DWMWA_CLOAKED, out var cloaked, 4);
                if (hr != unchecked((int)0x80070057) && (hr != 0 || cloaked != 0))
                    return IEResult.NOT_IE;
            }
            catch { }

            if (NativeMethods.IsZoomed(ie))
                return IEResult.INVALID;

            if (isIE(ie) && !NativeMethods.IsIconic(ie))
            {
                var ieRect = getIERect(ie);
                return ieRect.IntersectsWith(getScreenRect()) ? IEResult.IE : IEResult.IE_OUT_OF_BOUNDS;
            }
        }
        return IEResult.NOT_IE;
    }

    private static IntPtr findActiveIE()
    {
        activeIEobject = IntPtr.Zero;
        NativeMethods.EnumWindows((ie, _) =>
        {
            switch (isViableIE(ie))
            {
                case IEResult.IE:
                    activeIEobject = ie;
                    return false;
                case IEResult.IE_OUT_OF_BOUNDS:
                case IEResult.NOT_IE:
                    return true;
                default:
                    activeIEobject = IntPtr.Zero;
                    return false;
            }
        }, IntPtr.Zero);
        return activeIEobject;
    }

    private static Rectangle getIERect(IntPtr ie)
    {
        if (ie == IntPtr.Zero)
            return Rectangle.Empty;
        NativeMethods.GetWindowRect(ie, out var outer);
        var inner = new NativeMethods.RECT();
        if (getWindowRgnBox(ie, ref inner) == NativeMethods.ERROR)
        {
            inner.left = 0;
            inner.top = 0;
            inner.right = outer.Width;
            inner.bottom = outer.Height;
        }
        return new Rectangle(outer.left + inner.left, outer.top + inner.top, inner.Width, inner.Height);
    }

    private static int getWindowRgnBox(IntPtr window, ref NativeMethods.RECT rect)
    {
        var hRgn = NativeMethods.CreateRectRgn(0, 0, 0, 0);
        try
        {
            if (NativeMethods.GetWindowRgn(window, hRgn) == NativeMethods.ERROR)
                return NativeMethods.ERROR;
            NativeMethods.GetRgnBox(hRgn, out rect);
            return 1;
        }
        finally
        {
            NativeMethods.DeleteObject(hRgn);
        }
    }

    private static bool moveIE(IntPtr ie, Rectangle rect)
    {
        if (ie == IntPtr.Zero)
            return false;
        NativeMethods.GetWindowRect(ie, out var outer);
        var inner = new NativeMethods.RECT();
        if (getWindowRgnBox(ie, ref inner) == NativeMethods.ERROR)
        {
            inner.left = 0;
            inner.top = 0;
            inner.right = outer.Width;
            inner.bottom = outer.Height;
        }
        NativeMethods.MoveWindow(ie, rect.X - inner.left, rect.Y - inner.top,
            rect.Width + outer.Width - inner.Width, rect.Height + outer.Height - inner.Height, true);
        return true;
    }

    public override void tick()
    {
        base.tick();
        if (System.Environment.TickCount64 - lastCacheClear > 60_000)
        {
            refreshCache();
            lastCacheClear = System.Environment.TickCount64;
        }
        workArea.set(getWorkAreaRect());
        var ie = activeIEobject != IntPtr.Zero && isViableIE(activeIEobject) == IEResult.IE
            ? activeIEobject
            : findActiveIE();
        var ieRect = getIERect(ie);
        activeIE.setVisible(!ieRect.IsEmpty && ieRect.IntersectsWith(getScreen().toRectangle()));
        activeIE.set(ieRect.IsEmpty ? new Rectangle(-1, -1, 0, 0) : ieRect);
    }

    public override void dispose() { }

    public override void moveActiveIE(ScriptPoint point)
        => moveIE(findActiveIE(), new Rectangle(point.x, point.y, activeIE.getWidth(), activeIE.getHeight()));

    public override void restoreIE()
    {
        var offset = 25;
        NativeMethods.EnumWindows((ie, _) =>
        {
            if (isViableIE(ie) == IEResult.IE_OUT_OF_BOUNDS)
            {
                var work = new NativeMethods.RECT();
                NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETWORKAREA, 0, ref work, 0);
                NativeMethods.GetWindowRect(ie, out var rect);
                rect.Offset(work.left + offset - rect.left, work.top + offset - rect.top);
                NativeMethods.MoveWindow(ie, rect.left, rect.top, rect.Width, rect.Height, true);
                NativeMethods.BringWindowToTop(ie);
                offset += 25;
            }
            return true;
        }, IntPtr.Zero);
    }

    public override Area getWorkArea() => workArea;
    public override Area getActiveIE() => activeIE;

    public override string getActiveIETitle() => GetTitle(findActiveIE());

    private static Rectangle getWorkAreaRect()
    {
        var rect = new NativeMethods.RECT();
        NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETWORKAREA, 0, ref rect, 0);
        return new Rectangle(rect.left, rect.top, rect.Width, rect.Height);
    }

    public override void refreshCache()
    {
        ieCache.Clear();
        windowTitles = null;
        windowTitlesBlacklist = null;
    }
}
