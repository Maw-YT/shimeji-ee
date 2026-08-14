using System.Globalization;
using System.Text;

namespace GroupFinity.Mascot.I18n;

public sealed class PropertiesBundle
{
    private readonly Dictionary<string, string> _values;

    public PropertiesBundle(Dictionary<string, string> values)
    {
        _values = values;
    }

    public string getString(string key)
    {
        if (_values.TryGetValue(key, out var value))
            return value;
        return key;
    }

    public bool containsKey(string key) => _values.ContainsKey(key);

    public static PropertiesBundle GetBundle(string baseName, string languageTag)
    {
        var locale = languageTag.Replace('_', '-');
        CultureInfo culture;
        try { culture = CultureInfo.GetCultureInfo(locale); }
        catch { culture = CultureInfo.GetCultureInfo("en-GB"); }

        var files = CandidateFiles(baseName, culture);
        var merged = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            if (File.Exists(file))
            {
                foreach (var kv in LoadFile(file))
                    merged[kv.Key] = kv.Value;
            }
        }

        return new PropertiesBundle(merged);
    }

    private static IEnumerable<string> CandidateFiles(string baseName, CultureInfo culture)
    {
        var conf = Path.Combine(AppPaths.Root, "conf");
        var tags = new List<string>();
        var current = culture;
        while (!Equals(current, CultureInfo.InvariantCulture))
        {
            tags.Add(current.Name.Replace('-', '_'));
            if (string.IsNullOrEmpty(current.Parent.Name))
                break;
            current = current.Parent;
        }

        foreach (var tag in tags)
            yield return Path.Combine(conf, $"{baseName}_{tag}.properties");

        var lang = culture.TwoLetterISOLanguageName;
        yield return Path.Combine(conf, $"{baseName}_{lang}.properties");
        yield return Path.Combine(conf, $"{baseName}.properties");
    }

    public static Dictionary<string, string> LoadFile(string path)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var raw in File.ReadAllLines(path, Encoding.UTF8))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith('!'))
                continue;
            var idx = line.IndexOf('=');
            if (idx < 0)
                idx = line.IndexOf(':');
            if (idx <= 0)
                continue;
            var key = Unescape(line[..idx].Trim());
            var value = Unescape(line[(idx + 1)..].Trim());
            result[key] = value;
        }
        return result;
    }

    private static string Unescape(string text)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 1 < text.Length)
            {
                var next = text[++i];
                if (next == 'u' && i + 4 < text.Length)
                {
                    sb.Append((char)Convert.ToInt32(text.Substring(i + 1, 4), 16));
                    i += 4;
                }
                else if (next == 'n') sb.Append('\n');
                else if (next == 't') sb.Append('\t');
                else sb.Append(next);
            }
            else
            {
                sb.Append(text[i]);
            }
        }
        return sb.ToString();
    }
}
