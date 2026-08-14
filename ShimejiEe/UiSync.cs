namespace GroupFinity.Mascot;

/// <summary>
/// Marshals work onto the WinForms UI thread. Controls created off this thread
/// never get a message pump, which freezes the desktop windows.
/// </summary>
internal static class UiSync
{
    private static Control? host;

    public static void Init()
    {
        if (host != null)
            return;
        host = new Control();
        _ = host.Handle;
    }

    public static bool IsUiThread
        => host is { IsDisposed: false, IsHandleCreated: true } && !host.InvokeRequired;

    public static void Post(System.Action action)
    {
        if (host is { IsDisposed: false, IsHandleCreated: true } && host.InvokeRequired)
        {
            try { host.BeginInvoke(action); }
            catch (ObjectDisposedException) { }
            return;
        }
        action();
    }

    public static void Send(System.Action action)
    {
        if (host is { IsDisposed: false, IsHandleCreated: true } && host.InvokeRequired)
        {
            try { host.Invoke(action); }
            catch (ObjectDisposedException) { }
            return;
        }
        action();
    }

    public static T Send<T>(Func<T> func)
    {
        if (host is { IsDisposed: false, IsHandleCreated: true } && host.InvokeRequired)
        {
            T result = default!;
            host.Invoke(() => result = func());
            return result;
        }
        return func();
    }
}
