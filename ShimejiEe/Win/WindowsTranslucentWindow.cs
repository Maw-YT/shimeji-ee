using GroupFinity.Mascot.Image;

namespace GroupFinity.Mascot.Win;

internal sealed class WindowsTranslucentWindow : NativeWindow, TranslucentWindow
{
    private static readonly IntPtr ArrowCursor = NativeMethods.LoadCursor(IntPtr.Zero, (IntPtr)NativeMethods.IDC_ARROW);
    private static readonly IntPtr HandCursor = NativeMethods.LoadCursor(IntPtr.Zero, (IntPtr)NativeMethods.IDC_HAND);

    private WindowsNativeImage? image;
    private bool handCursor;
    private bool shown;
    private bool layeredReady;
    private int lastX;
    private int lastY;

    public event MouseEventHandler? MouseDown;
    public event MouseEventHandler? MouseUp;
    public event MouseEventHandler? MouseMove;

    public Point ScreenLocation => new(lastX, lastY);

    public WindowsTranslucentWindow()
    {
        var cp = new CreateParams
        {
            Caption = "Shimeji",
            X = 0,
            Y = 0,
            Width = 1,
            Height = 1,
            Style = NativeMethods.WS_POPUP,
            ExStyle = NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TOOLWINDOW |
                      NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOPMOST
        };
        CreateHandle(cp);
    }

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case NativeMethods.WM_PAINT:
            {
                NativeMethods.BeginPaint(Handle, out var ps);
                NativeMethods.EndPaint(Handle, ref ps);
                m.Result = 0;
                return;
            }
            case NativeMethods.WM_ERASEBKGND:
            case NativeMethods.WM_NCPAINT:
                m.Result = 1;
                return;
            case NativeMethods.WM_MOUSEACTIVATE:
                m.Result = (IntPtr)NativeMethods.MA_NOACTIVATE;
                return;
            case NativeMethods.WM_SETCURSOR:
                NativeMethods.SetCursor(handCursor ? HandCursor : ArrowCursor);
                m.Result = 1;
                return;
            case NativeMethods.WM_LBUTTONDOWN:
            case NativeMethods.WM_RBUTTONDOWN:
                NativeMethods.SetCapture(Handle);
                MouseDown?.Invoke(this, ToMouseArgs(m));
                return;
            case NativeMethods.WM_LBUTTONUP:
            case NativeMethods.WM_RBUTTONUP:
                NativeMethods.ReleaseCapture();
                MouseUp?.Invoke(this, ToMouseArgs(m));
                return;
            case NativeMethods.WM_MOUSEMOVE:
                MouseMove?.Invoke(this, ToMouseArgs(m));
                return;
        }
        base.WndProc(ref m);
    }

    private static MouseEventArgs ToMouseArgs(Message m)
    {
        var xy = unchecked((int)m.LParam.ToInt64());
        var x = (short)(xy & 0xFFFF);
        var y = (short)((xy >> 16) & 0xFFFF);
        var button = m.Msg switch
        {
            NativeMethods.WM_LBUTTONDOWN or NativeMethods.WM_LBUTTONUP => MouseButtons.Left,
            NativeMethods.WM_RBUTTONDOWN or NativeMethods.WM_RBUTTONUP => MouseButtons.Right,
            _ => MouseButtons.None
        };
        return new MouseEventArgs(button, 1, x, y, 0);
    }

    public void setAlwaysOnTop(bool value)
    {
        NativeMethods.SetWindowPos(Handle, new IntPtr(value ? NativeMethods.HWND_TOPMOST : 0),
            0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }

    public void setImage(NativeImage nativeImage) => image = (WindowsNativeImage)nativeImage;

    public void setHandCursor(bool useHand) => handCursor = useHand;

    public void updateImage()
    {
        if (image != null)
            present(image, lastX, lastY);
    }

    public void hide()
    {
        if (!UiSync.IsUiThread)
        {
            UiSync.Post(hide);
            return;
        }
        shown = false;
        NativeMethods.ShowWindow(Handle, NativeMethods.SW_HIDE);
    }

    public void present(NativeImage nativeImage, int x, int y)
    {
        image = (WindowsNativeImage)nativeImage;
        lastX = x;
        lastY = y;
        if (Handle == IntPtr.Zero)
            return;
        if (!UiSync.IsUiThread)
        {
            var captured = image;
            UiSync.Post(() => present(captured, x, y));
            return;
        }

        if (!layeredReady)
        {
            var exStyle = NativeMethods.GetWindowLong(Handle, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(Handle, NativeMethods.GWL_EXSTYLE, exStyle & ~NativeMethods.WS_EX_LAYERED);
            NativeMethods.SetWindowLong(Handle, NativeMethods.GWL_EXSTYLE,
                NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TOOLWINDOW |
                NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOPMOST);
            layeredReady = true;
        }

        var screenDC = NativeMethods.GetDC(IntPtr.Zero);
        var memDC = NativeMethods.CreateCompatibleDC(screenDC);
        var oldBmp = NativeMethods.SelectObject(memDC, image.Handle);
        try
        {
            var bf = new NativeMethods.BLENDFUNCTION
            {
                BlendOp = NativeMethods.BLENDFUNCTION.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = NativeMethods.BLENDFUNCTION.AC_SRC_ALPHA
            };
            var lt = new NativeMethods.POINT { x = x, y = y };
            var size = new NativeMethods.SIZE { cx = image.Width, cy = image.Height };
            var zero = new NativeMethods.POINT();
            NativeMethods.UpdateLayeredWindow(Handle, screenDC, ref lt, ref size, memDC, ref zero, 0, ref bf, NativeMethods.ULW_ALPHA);
        }
        finally
        {
            NativeMethods.SelectObject(memDC, oldBmp);
            NativeMethods.DeleteDC(memDC);
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDC);
        }

        if (!shown)
        {
            NativeMethods.ShowWindow(Handle, NativeMethods.SW_SHOWNA);
            shown = true;
        }
        NativeMethods.SetWindowPos(Handle, new IntPtr(NativeMethods.HWND_TOPMOST),
            0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }

    public void dispose()
    {
        if (!UiSync.IsUiThread)
        {
            UiSync.Post(dispose);
            return;
        }
        if (Handle != IntPtr.Zero)
            DestroyHandle();
    }
}
