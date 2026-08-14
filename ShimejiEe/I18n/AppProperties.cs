using System.Text;

namespace GroupFinity.Mascot.I18n;

public sealed class AppProperties
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public string getProperty(string key, string defaultValue = "")
        => _values.TryGetValue(key, out var value) ? value : defaultValue;

    public bool containsKey(string key) => _values.ContainsKey(key);

    public void setProperty(string key, string value) => _values[key] = value;

    public void remove(string key) => _values.Remove(key);

    public void load(string path)
    {
        if (!File.Exists(path))
            return;
        foreach (var kv in PropertiesBundle.LoadFile(path))
            _values[kv.Key] = kv.Value;
    }

    public void store(string path, string comments)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sb = new StringBuilder();
        sb.Append('#').AppendLine(comments);
        sb.Append('#').AppendLine(DateTime.Now.ToString("r"));
        foreach (var kv in _values.OrderBy(k => k.Key, StringComparer.Ordinal))
            sb.Append(kv.Key).Append('=').Append(Escape(kv.Value)).AppendLine();
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static string Escape(string text)
    {
        var sb = new StringBuilder();
        foreach (var c in text)
        {
            if (c == '\\') sb.Append("\\\\");
            else if (c == '\n') sb.Append("\\n");
            else if (c == '\r') { }
            else if (c == '\t') sb.Append("\\t");
            else sb.Append(c);
        }
        return sb.ToString();
    }
}
