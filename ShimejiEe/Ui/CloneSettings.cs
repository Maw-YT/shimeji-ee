namespace GroupFinity.Mascot.Ui;

internal static class CloneSettings
{
    public static void ShowDialog()
    {
        var main = Main.getInstance();
        var language = main.getLanguageBundle();
        using var form = new Form
        {
            Text = language.getString("MaxClonesPerSkin"),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            TopMost = true,
            ClientSize = new Size(340, 80)
        };

        var y = 12;
        var boxes = new List<(string Set, NumericUpDown Box)>();
        foreach (var imageSet in main.getLoadedImageSets().OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase))
        {
            if (imageSet.StartsWith('.'))
                continue;
            var label = new Label
            {
                Text = imageSet,
                Location = new Point(12, y + 3),
                AutoSize = true
            };
            var box = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 200,
                Value = main.getMaxClones(imageSet),
                Location = new Point(220, y),
                Width = 100
            };
            form.Controls.Add(label);
            form.Controls.Add(box);
            boxes.Add((imageSet, box));
            y += 32;
        }

        if (boxes.Count == 0)
        {
            form.Controls.Add(new Label { Text = "No skins loaded.", Location = new Point(12, 12), AutoSize = true });
            y = 44;
        }

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(164, y + 8), Size = new Size(75, 26) };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(245, y + 8), Size = new Size(75, 26) };
        form.Controls.Add(ok);
        form.Controls.Add(cancel);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        form.ClientSize = new Size(340, y + 46);

        if (form.ShowDialog() != DialogResult.OK)
            return;
        foreach (var (set, box) in boxes)
            main.setMaxClones(set, (int)box.Value);
    }
}
