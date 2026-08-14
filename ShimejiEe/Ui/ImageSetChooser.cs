namespace GroupFinity.Mascot.Ui;

public static class ImageSetChooser
{
    public static List<string>? Display()
    {
        var imgDir = AppPaths.Img();
        if (!Directory.Exists(imgDir))
        {
            MessageBox.Show(
                "No img folder was found.\nCopy a Shimeji-ee img directory next to the executable and try again.\n\nLooked in:\n" + imgDir,
                "Shimeji-ee", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        var sets = Directory.GetDirectories(imgDir)
            .Select(Path.GetFileName)
            .Where(n => n != null && !n.Equals("unused", StringComparison.OrdinalIgnoreCase) && !n.StartsWith('.'))
            .Cast<string>()
            .OrderBy(n => n)
            .ToList();

        if (sets.Count == 0)
        {
            MessageBox.Show("No image sets were found under img.", "Shimeji-ee", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        using var form = new Form
        {
            Text = Main.getInstance().getLanguageBundle().getString("ChooseShimeji"),
            Width = 420,
            Height = 480,
            StartPosition = FormStartPosition.CenterScreen
        };
        var list = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true };
        foreach (var set in sets)
            list.Items.Add(set, true);
        var ok = new Button { Text = "OK", Dock = DockStyle.Bottom, Height = 32, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Cancel", Dock = DockStyle.Bottom, Height = 32, DialogResult = DialogResult.Cancel };
        form.Controls.Add(list);
        form.Controls.Add(ok);
        form.Controls.Add(cancel);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        if (form.ShowDialog() != DialogResult.OK)
            return null;
        var selected = list.CheckedItems.Cast<string>().ToList();
        return selected.Count == 0 ? null : selected;
    }
}
