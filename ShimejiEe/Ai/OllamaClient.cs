using System.Drawing.Imaging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using GroupFinity.Mascot.Win;

namespace GroupFinity.Mascot.Ai;

internal static class OllamaClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(90) };
    private static readonly string[] VisionHints =
    {
        "vision", "llava", "bakllava", "minicpm", "moondream", "qwen2.5vl", "qwen2-vl",
        "gemma3", "pixtral", "llama3.2-vision", "llama4"
    };

    public static bool IsEnabled()
        => bool.TryParse(Main.getInstance().getProperties().getProperty("OllamaEnabled", "true"), out var on) && on;

    public static string BaseUrl()
        => Main.getInstance().getProperties().getProperty("OllamaUrl", "http://127.0.0.1:11434").TrimEnd('/');

    public static async Task<string?> ResolveModelAsync(CancellationToken ct)
    {
        var configured = Main.getInstance().getProperties().getProperty("OllamaModel", "").Trim();
        var names = await ListModelsAsync(ct);
        if (names.Count == 0) return string.IsNullOrEmpty(configured) ? null : configured;
        if (!string.IsNullOrEmpty(configured))
        {
            var match = names.FirstOrDefault(n => n.Equals(configured, StringComparison.OrdinalIgnoreCase))
                ?? names.FirstOrDefault(n => n.StartsWith(configured, StringComparison.OrdinalIgnoreCase));
            return match ?? configured;
        }
        var vision = names.FirstOrDefault(n => VisionHints.Any(h => n.Contains(h, StringComparison.OrdinalIgnoreCase)));
        return vision ?? names[0];
    }

    public static async Task<List<string>> ListModelsAsync(CancellationToken ct)
    {
        try
        {
            using var response = await Http.GetAsync(BaseUrl() + "/api/tags", ct);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var names = new List<string>();
            if (doc.RootElement.TryGetProperty("models", out var models))
            {
                foreach (var model in models.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out var name) && name.GetString() is { Length: > 0 } text)
                        names.Add(text);
                }
            }
            return names;
        }
        catch
        {
            return new List<string>();
        }
    }

    public static async Task<string> ChatAsync(string model, string system, IList<(string Role, string Content)> history,
        string user, byte[]? jpeg, CancellationToken ct)
    {
        var messages = new JsonArray
        {
            new JsonObject { ["role"] = "system", ["content"] = system }
        };
        foreach (var turn in history)
            messages.Add(new JsonObject { ["role"] = turn.Role, ["content"] = turn.Content });
        var userMessage = new JsonObject { ["role"] = "user", ["content"] = user };
        if (jpeg is { Length: > 0 })
            userMessage["images"] = new JsonArray { Convert.ToBase64String(jpeg) };
        messages.Add(userMessage);

        var payload = new JsonObject
        {
            ["model"] = model,
            ["stream"] = false,
            ["messages"] = messages,
            ["options"] = new JsonObject
            {
                ["temperature"] = 0.85,
                ["num_predict"] = 90
            }
        };

        using var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await Http.PostAsync(BaseUrl() + "/api/chat", content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException("Ollama " + (int)response.StatusCode + ": " + Trim(body, 240));
        using var doc = JsonDocument.Parse(body);
        var text = doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "";
        return Clean(text);
    }

    public static byte[]? CaptureScreenJpeg()
    {
        try
        {
            return UiSync.Send(() =>
            {
                var area = SystemInformation.VirtualScreen;
                using var full = new Bitmap(Math.Max(1, area.Width), Math.Max(1, area.Height));
                using (var graphics = Graphics.FromImage(full))
                    graphics.CopyFromScreen(area.Left, area.Top, 0, 0, full.Size);
                var width = 960;
                var height = Math.Max(1, full.Height * width / Math.Max(1, full.Width));
                using var small = new Bitmap(full, new Size(width, height));
                using var stream = new MemoryStream();
                small.Save(stream, ImageFormat.Jpeg);
                return stream.ToArray();
            });
        }
        catch (System.Exception e)
        {
            Log.Warning("Screen capture failed", e);
            return null;
        }
    }

    public static string WindowTitles()
    {
        var titles = new List<string>();
        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hwnd) || NativeMethods.IsIconic(hwnd))
                return true;
            var buffer = new StringBuilder(256);
            if (NativeMethods.GetWindowText(hwnd, buffer, buffer.Capacity) <= 0)
                return true;
            var title = buffer.ToString().Trim();
            if (title.Length == 0 || title.Equals("Shimeji", StringComparison.OrdinalIgnoreCase))
                return true;
            if (!titles.Contains(title, StringComparer.OrdinalIgnoreCase))
                titles.Add(title);
            return titles.Count < 12;
        }, IntPtr.Zero);
        return titles.Count == 0 ? "none visible" : string.Join("; ", titles);
    }

    public static string Clean(string text)
    {
        text = Regex.Replace(text, @"\s+", " ").Trim();
        text = text.Trim('"', '\'', '*', '`');
        if (text.Length > 280)
            text = text[..277].TrimEnd() + "...";
        return text;
    }

    private static string Trim(string text, int max)
        => text.Length <= max ? text : text[..max];
}
