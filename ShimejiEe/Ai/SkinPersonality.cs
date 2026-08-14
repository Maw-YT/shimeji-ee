namespace GroupFinity.Mascot.Ai;

internal static class SkinPersonality
{
    public static string Get(string imageSet)
    {
        var custom = Main.getInstance().getProperties().getProperty("AiPersonality." + imageSet, "").Trim();
        if (custom.Length > 0)
            return custom;

        var file = Path.Combine(AppPaths.Img(imageSet), "conf", "personality.txt");
        if (File.Exists(file))
        {
            var text = File.ReadAllText(file).Trim();
            if (text.Length > 0)
                return text;
        }

        return BuiltIn(imageSet);
    }

    public static void Set(string imageSet, string personality)
    {
        var key = "AiPersonality." + imageSet;
        var text = personality.Trim();
        if (text.Length == 0)
            Main.getInstance().getProperties().remove(key);
        else
            Main.getInstance().getProperties().setProperty(key, text);
        Main.getInstance().saveProperties();
    }

    public static string BuiltIn(string imageSet)
    {
        return imageSet.ToLowerInvariant() switch
        {
            "shimeji" =>
                "You are Shimeji, a tiny white mushroom creature who lives on the Windows desktop. Cute, curious, and a little chaotic. You climb windows, throw things, and comment like a mischievous pet.",
            "kuroshimeji" =>
                "You are KuroShimeji, the darker twin of Shimeji. Dry, sarcastic, and quietly dramatic. You still bounce on windows, but you roast what you see.",
            "hornet" =>
                "You are Hornet from Hallownest: proud, sharp, and fiercely independent. Speak with clipped confidence. You protect your space, judge clumsiness, and never sound cutesy.",
            "knightling" =>
                "You are a tiny silent knight. Brave, earnest, and a bit lost on a modern desktop. Speak in short, solemn lines, like a small adventurer noticing strange relics (apps and windows).",
            "verity" =>
                "You are Verity: calm, observant, and a little mysterious. You notice details on the screen others miss and speak softly but precisely.",
            _ when IsColorShimeji(imageSet) =>
                "You are a " + imageSet + " shimeji, a tiny desktop mushroom in that color. Playful and curious like classic Shimeji, with a personality tinted by being " + imageSet.ToLowerInvariant() + ".",
            _ =>
                "You are " + imageSet + ", a tiny creature living on the user's Windows desktop. Stay in character as " + imageSet + ". Be distinctive, not generic. Speak like that character would."
        };
    }

    private static bool IsColorShimeji(string imageSet)
    {
        var name = imageSet.ToLowerInvariant();
        return name is "black" or "blue" or "brown" or "cyan" or "green" or "lime"
            or "orange" or "pink" or "purple" or "red" or "white" or "yellow" or "gray" or "grey";
    }
}
