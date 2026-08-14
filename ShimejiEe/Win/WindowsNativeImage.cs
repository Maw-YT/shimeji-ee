using System.Drawing;
using GroupFinity.Mascot.Image;

namespace GroupFinity.Mascot.Win;

internal sealed class WindowsNativeImage : NativeImage, IDisposable
{
    public Bitmap Bitmap { get; }
    public IntPtr Handle { get; }
    public int Width => Bitmap.Width;
    public int Height => Bitmap.Height;

    public WindowsNativeImage(Bitmap image)
    {
        Bitmap = image;
        Handle = image.GetHbitmap(Color.FromArgb(0));
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
            NativeMethods.DeleteObject(Handle);
        Bitmap.Dispose();
    }
}
