using System.Text.RegularExpressions;
using GroupFinity.Mascot.Behavior;
using GroupFinity.Mascot.Config;
using GroupFinity.Mascot.I18n;

namespace GroupFinity.Mascot.Ui;

internal static class ActionDebug
{
    public static bool Enabled { get; set; }

    public static string Caption(string behaviorName, PropertiesBundle language)
    {
        if (language.containsKey(behaviorName))
            return language.getString(behaviorName);
        var spaced = Regex.Replace(behaviorName, @"([a-z])(IE)?([A-Z])", "$1 $2 $3");
        return Regex.Replace(spaced, " {2,}", " ").Trim();
    }

    public static IEnumerable<string> BehaviorNames(Configuration configuration)
        => configuration.getBehaviorNames()
            .Where(name => !name.Contains('/'))
            .OrderBy(name => Caption(name, Main.getInstance().getLanguageBundle()), StringComparer.CurrentCultureIgnoreCase);

    public static void FillForceMenu(ToolStripMenuItem parent, Mascot? target)
    {
        parent.DropDownItems.Clear();
        var language = Main.getInstance().getLanguageBundle();
        var names = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
        if (target != null)
        {
            foreach (var name in BehaviorNames(Main.getInstance().getConfiguration(target.imageSet)))
                names.Add(name);
        }
        else
        {
            foreach (var imageSet in Main.getInstance().getLoadedImageSets())
            {
                if (!Main.getInstance().tryGetConfiguration(imageSet, out var configuration))
                    continue;
                foreach (var name in BehaviorNames(configuration))
                    names.Add(name);
            }
        }

        if (target?.getBehavior() is UserBehavior current)
        {
            parent.DropDownItems.Add(new ToolStripMenuItem("Current: " + Caption(current.getName(), language))
            {
                Enabled = false
            });
            parent.DropDownItems.Add(new ToolStripSeparator());
        }

        foreach (var name in names)
        {
            var command = name;
            parent.DropDownItems.Add(Caption(command, language), null, (_, _) =>
            {
                if (target != null)
                    target.queueBehavior(command);
                else
                    Main.getInstance().getManager().forceBehaviorAll(command);
            });
        }

        parent.DropDown.MaximumSize = new Size(420, 480);
    }

    public static void FillAllowedMenu(ToolStripMenuItem parent, Mascot? target)
    {
        parent.DropDownItems.Clear();
        var language = Main.getInstance().getLanguageBundle();
        var names = new SortedSet<string>(StringComparer.CurrentCultureIgnoreCase);
        var imageSets = target != null
            ? new[] { target.imageSet }
            : Main.getInstance().getLoadedImageSets().ToArray();

        foreach (var imageSet in imageSets)
        {
            if (!Main.getInstance().tryGetConfiguration(imageSet, out var configuration))
                continue;
            foreach (var name in BehaviorNames(configuration))
            {
                if (configuration.isBehaviorToggleable(name))
                    names.Add(name);
            }
        }

        foreach (var name in names)
        {
            var command = name;
            var enabled = imageSets.All(imageSet =>
                !Main.getInstance().tryGetConfiguration(imageSet, out var configuration) ||
                !configuration.getBehaviorNames().Contains(command) ||
                configuration.isBehaviorEnabled(command, imageSet));
            var item = new ToolStripMenuItem(Caption(command, language))
            {
                CheckOnClick = true,
                Checked = enabled
            };
            item.Click += (_, _) =>
            {
                foreach (var imageSet in imageSets)
                    Main.getInstance().setMascotBehaviorEnabled(command, imageSet, item.Checked);
            };
            parent.DropDownItems.Add(item);
        }

        parent.DropDown.MaximumSize = new Size(420, 480);
    }
}
