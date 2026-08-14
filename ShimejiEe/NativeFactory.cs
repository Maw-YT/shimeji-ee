using System.Drawing;
using GroupFinity.Mascot.Environment;
using GroupFinity.Mascot.Image;
using GroupFinity.Mascot.Win;

namespace GroupFinity.Mascot;

public abstract class NativeFactory
{
    private static NativeFactory? instance;

    public static NativeFactory getInstance()
    {
        instance ??= new NativeFactoryImpl();
        return instance;
    }

    public static void resetInstance()
    {
        instance?.getEnvironment().dispose();
        instance = new NativeFactoryImpl();
    }

    public abstract Environment.Environment getEnvironment();
    public abstract NativeImage newNativeImage(Bitmap src);
    public abstract TranslucentWindow newTransparentWindow();
}

internal sealed class NativeFactoryImpl : NativeFactory
{
    private readonly Environment.Environment environment = new WindowsEnvironment();

    public override Environment.Environment getEnvironment() => environment;
    public override NativeImage newNativeImage(Bitmap src) => new WindowsNativeImage(src);
    public override TranslucentWindow newTransparentWindow()
        => UiSync.Send(() => new WindowsTranslucentWindow());
}
