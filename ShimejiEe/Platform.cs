namespace GroupFinity.Mascot;

public enum Platform
{
    x86 = 20,
    x86_64 = 24
}

public static class PlatformInfo
{
    public static Platform Current { get; } = System.Environment.Is64BitProcess ? Platform.x86_64 : Platform.x86;

    public static int BitmapSize => (int)Current;
}
