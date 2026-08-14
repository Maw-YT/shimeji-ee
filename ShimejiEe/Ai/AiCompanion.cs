using System.Collections.Concurrent;
using GroupFinity.Mascot.Ui;

namespace GroupFinity.Mascot.Ai;

internal static class AiCompanion
{
    private static readonly ConcurrentDictionary<int, List<(string Role, string Content)>> History = new();
    private static CancellationTokenSource? cts;
    private static int busy;

    public static void Start()
    {
        Stop();
        cts = new CancellationTokenSource();
        var token = cts.Token;
        _ = Task.Run(() => IdleLoop(token), token);
    }

    public static void Stop()
    {
        try { cts?.Cancel(); } catch { }
        cts = null;
    }

    public static void PromptUser(Mascot mascot)
    {
        var typed = RespondDialog.Ask();
        if (string.IsNullOrWhiteSpace(typed))
            return;
        mascot.say("...");
        _ = Task.Run(async () =>
        {
            try
            {
                var reply = await Talk(mascot, typed.Trim(), idle: false, CancellationToken.None);
                mascot.say(reply);
            }
            catch (System.Exception e)
            {
                Log.Warning("Ollama reply failed", e);
                mascot.say(Main.getInstance().getLanguageBundle().getString("OllamaUnavailable"));
            }
        });
    }

    private static async Task IdleLoop(CancellationToken token)
    {
        try { await Task.Delay(18000, token); }
        catch (OperationCanceledException) { return; }

        while (!token.IsCancellationRequested)
        {
            try
            {
                if (OllamaClient.IsEnabled())
                {
                    var mascots = Main.getInstance().getManager().snapshot();
                    if (mascots.Count > 0)
                    {
                        var mascot = mascots[Random.Shared.Next(mascots.Count)];
                        mascot.say("...");
                        var reply = await Talk(mascot, null, idle: true, token);
                        if (!string.IsNullOrWhiteSpace(reply))
                            mascot.say(reply);
                        else
                            mascot.hideSpeech();
                    }
                }
            }
            catch (OperationCanceledException) { return; }
            catch (System.Exception e)
            {
                Log.Warning("Ollama idle talk failed", e);
            }

            try { await Task.Delay(TimeSpan.FromSeconds(70 + Random.Shared.Next(50)), token); }
            catch (OperationCanceledException) { return; }
        }
    }

    private static async Task<string> Talk(Mascot mascot, string? userText, bool idle, CancellationToken token)
    {
        if (idle)
        {
            if (Interlocked.CompareExchange(ref busy, 1, 0) != 0)
                return "";
        }
        else
            Interlocked.Exchange(ref busy, 1);
        try
        {
            var model = await OllamaClient.ResolveModelAsync(token);
            if (string.IsNullOrEmpty(model))
                return Main.getInstance().getLanguageBundle().getString("OllamaUnavailable");

            var jpeg = OllamaClient.CaptureScreenJpeg();
            var windows = OllamaClient.WindowTitles();
            var system =
                "You are a tiny desktop creature named " + mascot.imageSet + " living on the user's Windows desktop. " +
                "Personality:\n" + SkinPersonality.Get(mascot.imageSet) + "\n" +
                "You can see their screen (image attached when available) and open window titles. " +
                "Reply in 1-2 short spoken sentences fully in character. No lists, no markdown, no stage directions.";
            var user = idle
                ? "You glanced at the desktop. React briefly to what you see.\nOpen windows: " + windows
                : "The user said: \"" + userText + "\"\nOpen windows: " + windows;

            var history = History.GetOrAdd(mascot.Id, _ => new List<(string, string)>());
            List<(string Role, string Content)> snapshot;
            lock (history)
                snapshot = history.TakeLast(8).ToList();

            var reply = await OllamaClient.ChatAsync(model, system, snapshot, user, jpeg, token);
            if (string.IsNullOrWhiteSpace(reply))
                return "...";

            lock (history)
            {
                if (!string.IsNullOrWhiteSpace(userText))
                    history.Add(("user", userText));
                history.Add(("assistant", reply));
                while (history.Count > 12)
                    history.RemoveAt(0);
            }
            return reply;
        }
        finally
        {
            Interlocked.Exchange(ref busy, 0);
        }
    }
}
