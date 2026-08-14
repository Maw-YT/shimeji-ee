using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace GroupFinity.Mascot.Image;

public static class ImagePairLoader
{
    public enum Filter { NEAREST_NEIGHBOUR, HQX, BICUBIC }

    public static void load(string path, string? rightPath, Point center, double scaling, Filter filter, double opacity)
    {
        var key = path + (rightPath ?? "");
        if (ImagePairs.contains(key))
            return;

        using var leftSrc = (Bitmap)System.Drawing.Image.FromFile(path);
        var leftImage = scale(premultiply(leftSrc, opacity), scaling, filter);
        Bitmap rightImage;
        if (rightPath == null)
            rightImage = flip(leftImage);
        else
        {
            using var rightSrc = (Bitmap)System.Drawing.Image.FromFile(rightPath);
            rightImage = scale(premultiply(rightSrc, opacity), scaling, filter);
        }

        var ip = new ImagePair(
            new MascotImage(leftImage, new Point((int)Math.Round(center.X * scaling), (int)Math.Round(center.Y * scaling))),
            new MascotImage(rightImage, new Point(rightImage.Width - (int)Math.Round(center.X * scaling), (int)Math.Round(center.Y * scaling))));
        ImagePairs.load(key, ip);
    }

    private static Bitmap flip(Bitmap src)
    {
        var copy = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppPArgb);
        var srcData = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
        var destData = copy.LockBits(new Rectangle(0, 0, copy.Width, copy.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
        try
        {
            unsafe
            {
                var width = src.Width;
                var height = src.Height;
                for (var y = 0; y < height; y++)
                {
                    var srcRow = (int*)((byte*)srcData.Scan0 + y * srcData.Stride);
                    var destRow = (int*)((byte*)destData.Scan0 + y * destData.Stride);
                    for (var x = 0; x < width; x++)
                        destRow[width - x - 1] = srcRow[x];
                }
            }
        }
        finally
        {
            src.UnlockBits(srcData);
            copy.UnlockBits(destData);
        }
        return copy;
    }

    private static Bitmap premultiply(Bitmap source, double opacity)
    {
        var rect = new Rectangle(0, 0, source.Width, source.Height);
        var dest = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppPArgb);
        var srcData = source.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var destData = dest.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
        try
        {
            unsafe
            {
                for (var y = 0; y < dest.Height; y++)
                {
                    var srcRow = (byte*)srcData.Scan0 + y * srcData.Stride;
                    var destRow = (byte*)destData.Scan0 + y * destData.Stride;
                    for (var x = 0; x < dest.Width; x++)
                    {
                        var b = srcRow[x * 4 + 0];
                        var g = srcRow[x * 4 + 1];
                        var r = srcRow[x * 4 + 2];
                        var a = srcRow[x * 4 + 3] / 255f * (float)opacity;
                        destRow[x * 4 + 0] = (byte)Math.Round(b * a);
                        destRow[x * 4 + 1] = (byte)Math.Round(g * a);
                        destRow[x * 4 + 2] = (byte)Math.Round(r * a);
                        destRow[x * 4 + 3] = (byte)Math.Round(a * 255);
                    }
                }
            }
        }
        finally
        {
            source.UnlockBits(srcData);
            dest.UnlockBits(destData);
        }
        return dest;
    }

    private static Bitmap scale(Bitmap source, double scaling, Filter filter)
    {
        if (Math.Abs(scaling - 1) < 0.0001)
            return source;

        var width = (int)Math.Round(source.Width * scaling);
        var height = (int)Math.Round(source.Height * scaling);
        var copy = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
        using var g = Graphics.FromImage(copy);
        g.InterpolationMode = filter == Filter.BICUBIC ? InterpolationMode.HighQualityBicubic : InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.DrawImage(source, 0, 0, width, height);
        return copy;
    }
}
