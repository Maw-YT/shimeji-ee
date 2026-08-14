using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace GroupFinity.Mascot.Ui;

internal sealed class SpeechBubble : Form
{
    private readonly System.Windows.Forms.Timer hideTimer = new() { Interval = 14000 };
    private string text = "";

    public SpeechBubble()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(255, 255, 236, 150);
        ForeColor = Color.FromArgb(40, 32, 24);
        Padding = new Padding(0);
        hideTimer.Tick += (_, _) => Hide();
        Click += (_, _) => Hide();
        DoubleBuffered = true;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= Win.NativeMethods.WS_EX_TOOLWINDOW | Win.NativeMethods.WS_EX_NOACTIVATE | Win.NativeMethods.WS_EX_TOPMOST;
            return cp;
        }
    }

    public void ShowText(string value)
    {
        text = string.IsNullOrWhiteSpace(value) ? "..." : value.Trim();
        using var font = BubbleFont();
        var size = TextRenderer.MeasureText(text, font, new Size(240, 0),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        Width = Math.Clamp(size.Width + 28, 80, 280);
        Height = Math.Clamp(size.Height + 36, 48, 220);
        using var path = Rounded(Width, Height);
        Region = new Region(path);
        hideTimer.Stop();
        hideTimer.Start();
        if (!Visible)
            Show();
        Invalidate();
    }

    public void Follow(Rectangle mascotBounds)
    {
        if (!Visible || mascotBounds.IsEmpty) return;
        var x = mascotBounds.X + mascotBounds.Width / 2 - Width / 2;
        var y = mascotBounds.Y - Height - 6;
        var screen = Screen.FromPoint(new Point(mascotBounds.X, mascotBounds.Y)).WorkingArea;
        if (y < screen.Top)
            y = mascotBounds.Bottom + 6;
        x = Math.Clamp(x, screen.Left, screen.Right - Width);
        y = Math.Clamp(y, screen.Top, screen.Bottom - Height);
        Location = new Point(x, y);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        using var fill = new SolidBrush(BackColor);
        using var border = new Pen(Color.FromArgb(70, 50, 20), 2);
        using var path = Rounded(Width, Height);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
        using var font = BubbleFont();
        TextRenderer.DrawText(e.Graphics, text, font, new Rectangle(12, 10, Width - 24, Height - 22),
            ForeColor, TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) hideTimer.Dispose();
        base.Dispose(disposing);
    }

    private static Font BubbleFont() => new("Segoe UI", 9f, FontStyle.Regular);

    private static GraphicsPath Rounded(int width, int height)
    {
        const int r = 14;
        var path = new GraphicsPath();
        path.AddArc(1, 1, r, r, 180, 90);
        path.AddArc(width - r - 2, 1, r, r, 270, 90);
        path.AddArc(width - r - 2, height - r - 2, r, r, 0, 90);
        path.AddArc(1, height - r - 2, r, r, 90, 90);
        path.CloseFigure();
        return path;
    }
}
