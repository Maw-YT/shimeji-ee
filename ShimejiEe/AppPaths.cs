namespace GroupFinity.Mascot;

public static class AppPaths
{
    public static string Root { get; private set; } = FindRoot();

    public static void Use(string root)
    {
        Root = root;
        Directory.SetCurrentDirectory(root);
    }

    public static string Conf(params string[] parts) => Path.Combine(new[] { Root, "conf" }.Concat(parts).ToArray());

    public static string Img(params string[] parts) => Path.Combine(new[] { Root, "img" }.Concat(parts).ToArray());

    private static string FindRoot()
    {
        var candidates = new List<string>();
        var baseDir = AppContext.BaseDirectory;
        candidates.Add(baseDir);
        candidates.Add(Directory.GetCurrentDirectory());

        var dir = new DirectoryInfo(baseDir);
        for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            candidates.Add(dir.FullName);
            candidates.Add(Path.Combine(dir.FullName, "ogshimejieesrc"));
        }

        foreach (var candidate in candidates.Distinct())
        {
            if (Directory.Exists(Path.Combine(candidate, "conf")) &&
                File.Exists(Path.Combine(candidate, "conf", "actions.xml")))
            {
                return candidate;
            }
        }

        return baseDir;
    }
}
