namespace GroupFinity.Mascot;

public static class Log
{
    private static readonly object Sync = new();
    private static readonly string LogFile = Path.Combine(AppContext.BaseDirectory, "shimeji-ee.log");

    public static void Info(string message) => Write("INFO", message, null);

    public static void Warning(string message, System.Exception? ex = null) => Write("WARN", message, ex);

    public static void Severe(string message, System.Exception? ex = null) => Write("SEVERE", message, ex);

    private static void Write(string level, string message, System.Exception? ex)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
        if (ex != null)
            line += System.Environment.NewLine + ex;
        lock (Sync)
        {
            try { File.AppendAllText(LogFile, line + System.Environment.NewLine); }
            catch { /* ignore log IO errors */ }
            Console.WriteLine(line);
        }
    }
}
