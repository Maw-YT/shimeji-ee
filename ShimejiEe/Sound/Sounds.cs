using System.Collections.Concurrent;

namespace GroupFinity.Mascot.Sound;

public sealed class SoundClip
{
    private readonly string path;
    public SoundClip(string path) { this.path = path; }
    public bool IsRunning { get; private set; }
    public void Stop() { IsRunning = false; }
    public void Play()
    {
        try
        {
            var player = new System.Media.SoundPlayer(path);
            player.Play();
            IsRunning = true;
        }
        catch { }
    }
}

public static class Sounds
{
    private static readonly ConcurrentDictionary<string, SoundClip> SOUNDS = new();

    public static void load(string filename, SoundClip clip) => SOUNDS.TryAdd(filename, clip);
    public static bool contains(string filename) => SOUNDS.ContainsKey(filename);
    public static SoundClip? getSound(string filename) => SOUNDS.TryGetValue(filename, out var c) ? c : null;

    public static bool isMuted()
        => !bool.Parse(Main.getInstance().getProperties().getProperty("Sounds", "true"));

    public static void setMuted(bool mutedFlag)
    {
        if (!mutedFlag) return;
        foreach (var clip in SOUNDS.Values)
            clip.Stop();
    }
}

public static class SoundLoader
{
    public static void load(string name, float volume)
    {
        var key = name + volume;
        if (Sounds.contains(key))
            return;
        Sounds.load(key, new SoundClip(name));
    }
}
