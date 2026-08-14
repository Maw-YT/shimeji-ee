using GroupFinity.Mascot.Ai;

namespace GroupFinity.Mascot.Ui;

internal static class PersonalitySettings
{
    public static void ShowDialog()
    {
        var main = Main.getInstance();
        var language = main.getLanguageBundle();
        var skins = main.getLoadedImageSets()
            .Where(s => !s.StartsWith('.'))
            .OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        using var form = new Form
        {
            Text = language.getString("AiPersonalities"),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            TopMost = true,
            ClientSize = new Size(460, 280)
        };

        var hint = new Label
        {
            Text = "Each skin talks with its own personality.",
            Location = new Point(12, 10),
            AutoSize = true
        };
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(12, 34),
            Width = 436
        };
        var box = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Location = new Point(12, 66),
            Size = new Size(436, 160)
        };
        var reset = new Button { Text = "Reset", Location = new Point(12, 238), Size = new Size(75, 26) };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(292, 238), Size = new Size(75, 26) };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(373, 238), Size = new Size(75, 26) };

        var drafts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var skin in skins)
        {
            combo.Items.Add(skin);
            drafts[skin] = SkinPersonality.Get(skin);
        }

        string? selected = null;
        void ShowSkin(string skin)
        {
            if (selected != null)
                drafts[selected] = box.Text;
            selected = skin;
            box.Text = drafts[skin];
        }

        combo.SelectedIndexChanged += (_, _) =>
        {
            if (combo.SelectedItem is string skin)
                ShowSkin(skin);
        };
        reset.Click += (_, _) =>
        {
            if (selected == null) return;
            box.Text = SkinPersonality.BuiltIn(selected);
            drafts[selected] = box.Text;
        };

        form.Controls.AddRange(new Control[] { hint, combo, box, reset, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        if (combo.Items.Count > 0)
            combo.SelectedIndex = 0;

        if (form.ShowDialog() != DialogResult.OK)
            return;
        if (selected != null)
            drafts[selected] = box.Text;
        foreach (var kv in drafts)
        {
            var builtIn = SkinPersonality.BuiltIn(kv.Key);
            SkinPersonality.Set(kv.Key, string.Equals(kv.Value.Trim(), builtIn, StringComparison.Ordinal) ? "" : kv.Value);
        }
    }
}
