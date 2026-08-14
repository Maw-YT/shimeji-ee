using System.Collections.Concurrent;
using System.Drawing;

namespace GroupFinity.Mascot.Image;

public interface NativeImage
{
    int Width { get; }
    int Height { get; }
}

public interface TranslucentWindow
{
    event MouseEventHandler MouseDown;
    event MouseEventHandler MouseUp;
    event MouseEventHandler MouseMove;
    void setAlwaysOnTop(bool value);
    void setImage(NativeImage image);
    void updateImage();
    void present(NativeImage image, int x, int y);
    void setHandCursor(bool useHand);
    void hide();
    Point ScreenLocation { get; }
    void dispose();
}

public sealed class MascotImage
{
    public NativeImage Image { get; }
    public Point Center { get; }
    public Size Size { get; }

    public MascotImage(NativeImage image, Point center, Size size)
    {
        Image = image;
        Center = center;
        Size = size;
    }

    public MascotImage(Bitmap bitmap, Point center)
        : this(NativeFactory.getInstance().newNativeImage(bitmap), center, bitmap.Size)
    {
    }

    public NativeImage getImage() => Image;
    public Point getCenter() => Center;
    public Size getSize() => Size;
}

public sealed class ImagePair
{
    private readonly MascotImage leftImage;
    private readonly MascotImage rightImage;

    public ImagePair(MascotImage leftImage, MascotImage rightImage)
    {
        this.leftImage = leftImage;
        this.rightImage = rightImage;
    }

    public MascotImage getImage(bool lookRight) => lookRight ? rightImage : leftImage;
}

public static class ImagePairs
{
    private static readonly ConcurrentDictionary<string, ImagePair> imagepairs = new();

    public static void load(string filename, ImagePair imagepair) => imagepairs.TryAdd(filename, imagepair);

    public static ImagePair? getImagePair(string filename)
        => imagepairs.TryGetValue(filename, out var ip) ? ip : null;

    public static bool contains(string filename) => imagepairs.ContainsKey(filename);

    public static void clear() => imagepairs.Clear();

    public static void removeAll(string searchTerm)
    {
        foreach (var filename in imagepairs.Keys.ToList())
        {
            try
            {
                var parts = filename.Replace('\\', '/').Split('/');
                if (parts.Length > 2 && parts[2] == searchTerm)
                    imagepairs.TryRemove(filename, out _);
            }
            catch { }
        }
    }

    public static MascotImage? getImage(string filename, bool isLookRight)
        => getImagePair(filename)?.getImage(isLookRight);
}
