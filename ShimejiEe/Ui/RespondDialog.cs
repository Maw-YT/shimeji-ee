namespace GroupFinity.Mascot.Ui;

internal static class RespondDialog
{
    public static string? Ask()
    {
        using var form = new Form
        {
            Text = Main.getInstance().getLanguageBundle().getString("Respond"),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            TopMost = true,
            ClientSize = new Size(360, 118)
        };
        var box = new TextBox
        {
            Multiline = true,
            AcceptsReturn = false,
            Location = new Point(12, 12),
            Size = new Size(336, 58)
        };
        var ok = new Button { Text = "Send", DialogResult = DialogResult.OK, Location = new Point(192, 80), Size = new Size(75, 26) };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(273, 80), Size = new Size(75, 26) };
        form.Controls.AddRange(new Control[] { box, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        form.Shown += (_, _) => box.Focus();
        return form.ShowDialog() == DialogResult.OK ? box.Text : null;
    }
}
