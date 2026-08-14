using System.Drawing;
using System.Xml;
using GroupFinity.Mascot.Config;
using GroupFinity.Mascot.I18n;
using GroupFinity.Mascot.Image;
using GroupFinity.Mascot.Sound;
using GroupFinity.Mascot.Ui;

namespace GroupFinity.Mascot;

public sealed class Main
{
    public const string BEHAVIOR_GATHER = "ChaseMouse";
    private static readonly Main instance = new();
    private readonly Manager manager = new();
    private readonly List<string> imageSets = new();
    private readonly Dictionary<string, Configuration> configurations = new();
    private readonly Dictionary<string, List<string>> childImageSets = new();
    private readonly AppProperties properties = new();
    private PropertiesBundle languageBundle = new(new Dictionary<string, string>());
    private NotifyIcon? tray;

    public static Main getInstance() => instance;

    public static void showError(string message, System.Exception? exception = null)
    {
        if (exception != null)
        {
            var e = exception;
            while (e != null)
            {
                message += "\n" + e.Message;
                e = e.InnerException;
            }
            message += "\n" + instance.languageBundle.getString("SeeLogForDetails");
        }
        var text = message;
        UiSync.Post(() => MessageBox.Show(text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error));
    }

    [STAThread]
    public static void MainEntry(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Directory.SetCurrentDirectory(AppPaths.Root);
        UiSync.Init();
        getInstance().run();
        Application.Run();
    }

    public void run()
    {
        properties.load(AppPaths.Conf("settings.properties"));
        try
        {
            languageBundle = PropertiesBundle.GetBundle("language", properties.getProperty("Language", "en-GB"));
        }
        catch
        {
            showError("The default language file could not be loaded. Ensure that you have the latest shimeji language.properties in your conf directory.");
            exit();
            return;
        }

        if (!bool.Parse(properties.getProperty("AlwaysShowShimejiChooser", "false")))
        {
            foreach (var set in properties.getProperty("ActiveShimeji", "").Split('/'))
            {
                if (!string.IsNullOrWhiteSpace(set))
                    imageSets.Add(set.Trim());
            }
        }

        do
        {
            if (imageSets.Count == 0)
            {
                var chosen = ImageSetChooser.Display();
                if (chosen == null)
                {
                    exit();
                    return;
                }
                imageSets.Clear();
                imageSets.AddRange(chosen);
            }
            for (var index = 0; index < imageSets.Count; index++)
            {
                if (!loadConfiguration(imageSets[index]))
                {
                    configurations.Remove(imageSets[index]);
                    imageSets.RemoveAt(index);
                    index--;
                }
            }
        } while (imageSets.Count == 0);

        createTrayIcon();
        foreach (var imageSet in imageSets)
            createMascot(imageSet);
        manager.start();
        Ai.AiCompanion.Start();
    }

    private bool loadConfiguration(string imageSet)
    {
        try
        {
            var actionsPath = ResolveXml(imageSet, true);
            var behaviorsPath = ResolveXml(imageSet, false);
            Log.Info($"{imageSet} Read Action File ({actionsPath})");
            var actions = LoadXml(actionsPath);
            var configuration = new Configuration();
            configuration.load(new Entry(actions), imageSet);
            Log.Info($"{imageSet} Read Behavior File ({behaviorsPath})");
            var behaviors = LoadXml(behaviorsPath);
            configuration.load(new Entry(behaviors), imageSet);
            configuration.validate();
            configurations[imageSet] = configuration;

            var childMascots = new List<string>();
            foreach (var list in new Entry(actions).selectChildren("ActionList"))
            {
                foreach (var node in list.selectChildren("Action"))
                {
                    foreach (var attr in new[] { "BornMascot", "TransformMascot" })
                    {
                        if (node.getAttributes().TryGetValue(attr, out var set) && !string.IsNullOrEmpty(set))
                        {
                            if (!childMascots.Contains(set)) childMascots.Add(set);
                            if (!configurations.ContainsKey(set)) loadConfiguration(set);
                        }
                    }
                }
            }
            childImageSets[imageSet] = childMascots;
            return true;
        }
        catch (System.Exception e)
        {
            Log.Severe("Failed to load configuration files", e);
            showError(languageBundle.getString("FailedLoadConfigErrorMessage"), e);
            return false;
        }
    }

    private static XmlElement LoadXml(string path)
    {
        var doc = new XmlDocument();
        doc.Load(path);
        return doc.DocumentElement!;
    }

    private static string ResolveXml(string imageSet, bool actions)
    {
        var names = actions
            ? new[] { "actions.xml", "動作.xml", "one.xml", "1.xml" }
            : new[] { "behaviors.xml", "behavior.xml", "行動.xml", "two.xml", "2.xml" };
        var dirs = new[]
        {
            AppPaths.Conf(imageSet),
            Path.Combine(AppPaths.Img(imageSet), "conf"),
            AppPaths.Conf()
        };
        foreach (var dir in dirs)
        {
            foreach (var name in names)
            {
                var candidate = Path.Combine(dir, name);
                if (File.Exists(candidate)) return candidate;
            }
        }
        return Path.Combine(AppPaths.Conf(), actions ? "actions.xml" : "behaviors.xml");
    }

    private void createTrayIcon()
    {
        System.Drawing.Image iconImage;
        try { iconImage = System.Drawing.Image.FromFile(AppPaths.Img("icon.png")); }
        catch { iconImage = new Bitmap(16, 16); }

        tray = new NotifyIcon
        {
            Icon = Icon.FromHandle(new Bitmap(iconImage, new Size(16, 16)).GetHicon()),
            Text = properties.getProperty("ShimejiEENameOverride", "").Trim() is { Length: > 0 } cap ? cap : languageBundle.getString("ShimejiEE"),
            Visible = true
        };
        tray.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                createMascot();
        };
        var menu = new ContextMenuStrip();
        menu.Items.Add(languageBundle.getString("CallShimeji"), null, (_, _) => createMascot());
        menu.Items.Add(languageBundle.getString("FollowCursor"), null, (_, _) => manager.setBehaviorAll(BEHAVIOR_GATHER));
        menu.Items.Add(languageBundle.getString("ReduceToOne"), null, (_, _) => manager.remainOne());
        menu.Items.Add(languageBundle.getString("RestoreWindows"), null, (_, _) => NativeFactory.getInstance().getEnvironment().restoreIE());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(languageBundle.getString("ChooseShimeji"), null, (_, _) =>
        {
            var chosen = ImageSetChooser.Display();
            if (chosen != null) setActiveImageSets(chosen);
        });
        menu.Items.Add(languageBundle.getString("PauseAnimations"), null, (_, _) => manager.togglePauseAll());
        var breedingToggle = new ToolStripMenuItem(languageBundle.getString("BreedingCloning"))
        {
            CheckOnClick = true,
            Checked = isFlag("Breeding", true)
        };
        breedingToggle.CheckedChanged += (_, _) =>
        {
            properties.setProperty("Breeding", breedingToggle.Checked ? "true" : "false");
            saveProperties();
        };
        menu.Items.Add(breedingToggle);
        var transientToggle = new ToolStripMenuItem(languageBundle.getString("BreedingTransient"))
        {
            CheckOnClick = true,
            Checked = isFlag("Transients", true)
        };
        transientToggle.CheckedChanged += (_, _) =>
        {
            properties.setProperty("Transients", transientToggle.Checked ? "true" : "false");
            saveProperties();
        };
        menu.Items.Add(transientToggle);
        menu.Items.Add(languageBundle.getString("MaxClonesPerSkin"), null, (_, _) => Ui.CloneSettings.ShowDialog());
        var debugToggle = new ToolStripMenuItem(languageBundle.getString("ActionDebug")) { CheckOnClick = true };
        var forceMenu = new ToolStripMenuItem(languageBundle.getString("SetBehaviour")) { Enabled = false };
        forceMenu.DropDownOpening += (_, _) => Ui.ActionDebug.FillForceMenu(forceMenu, null);
        debugToggle.CheckedChanged += (_, _) =>
        {
            Ui.ActionDebug.Enabled = debugToggle.Checked;
            forceMenu.Enabled = debugToggle.Checked;
        };
        menu.Items.Add(debugToggle);
        menu.Items.Add(forceMenu);
        var allowedMenu = new ToolStripMenuItem(languageBundle.getString("AllowedBehaviours"));
        allowedMenu.DropDownOpening += (_, _) => Ui.ActionDebug.FillAllowedMenu(allowedMenu, null);
        menu.Items.Add(allowedMenu);
        var ollamaToggle = new ToolStripMenuItem(languageBundle.getString("OllamaTalk"))
        {
            CheckOnClick = true,
            Checked = bool.TryParse(properties.getProperty("OllamaEnabled", "true"), out var ollamaOn) && ollamaOn
        };
        ollamaToggle.CheckedChanged += (_, _) =>
        {
            properties.setProperty("OllamaEnabled", ollamaToggle.Checked ? "true" : "false");
            saveProperties();
        };
        menu.Items.Add(ollamaToggle);
        menu.Items.Add(languageBundle.getString("AiPersonalities"), null, (_, _) => Ui.PersonalitySettings.ShowDialog());
        menu.Items.Add(languageBundle.getString("DismissAll"), null, (_, _) => exit());
        tray.ContextMenuStrip = menu;
        Sounds.setMuted(!bool.Parse(properties.getProperty("Sounds", "true")));
    }

    public void createMascot()
    {
        if (imageSets.Count == 0) return;
        createMascot(imageSets[Random.Shared.Next(imageSets.Count)]);
    }

    public void createMascot(string imageSet)
    {
        if (!canSpawn(imageSet, fromBreed: false, transient: false))
            return;
        var mascot = new Mascot(imageSet)
        {
            anchor = new ScriptPoint(-4000, -4000),
            lookRight = Random.Shared.NextDouble() < 0.5
        };
        try
        {
            mascot.setBehavior(getConfiguration(imageSet).buildNextBehavior(null, mascot));
            manager.add(mascot);
        }
        catch (System.Exception e)
        {
            Log.Severe(imageSet + " fatal error, can not be started.", e);
            showError(languageBundle.getString("CouldNotCreateShimejiErrorMessage") + " " + imageSet, e);
            mascot.dispose();
        }
    }

    private void setActiveImageSets(List<string> newImageSets)
    {
        var toRemove = imageSets.Except(newImageSets).ToList();
        var toAdd = newImageSets.Except(imageSets).ToList();
        var isExit = manager.isExitOnLastRemoved();
        manager.setExitOnLastRemoved(false);
        foreach (var r in toRemove)
        {
            imageSets.Remove(r);
            manager.remainNone(r);
            configurations.Remove(r);
            ImagePairs.removeAll(r);
        }
        foreach (var a in toAdd)
        {
            if (loadConfiguration(a))
            {
                imageSets.Add(a);
                createMascot(a);
            }
        }
        manager.setExitOnLastRemoved(isExit);
    }

    public bool tryGetConfiguration(string imageSet, out Configuration configuration)
        => configurations.TryGetValue(imageSet, out configuration!);

    public IEnumerable<string> getLoadedImageSets() => configurations.Keys;

    public Configuration getConfiguration(string imageSet) => configurations[imageSet];

    public void setMascotBehaviorEnabled(string name, string imageSet, bool enabled)
    {
        var key = "DisabledBehaviours." + imageSet;
        var list = properties.getProperty(key, "")
            .Split('/')
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        if (enabled)
            list.RemoveAll(s => s == name);
        else if (!list.Contains(name))
            list.Add(name);
        if (list.Count > 0)
            properties.setProperty(key, string.Join('/', list));
        else
            properties.remove(key);
        saveProperties();
    }

    public void saveProperties()
    {
        try { properties.store(AppPaths.Conf("settings.properties"), "Shimeji-ee Configuration Options"); }
        catch (System.Exception e) { Log.Warning("Failed to save settings", e); }
    }

    public bool isFlag(string key, bool defaultValue)
        => bool.TryParse(properties.getProperty(key, defaultValue ? "true" : "false"), out var value) ? value : defaultValue;

    public int getMaxClones(string imageSet)
    {
        if (int.TryParse(properties.getProperty("MaxClones." + imageSet, ""), out var perSkin) && perSkin > 0)
            return perSkin;
        if (int.TryParse(properties.getProperty("MaxClones", "12"), out var global) && global > 0)
            return global;
        return 12;
    }

    public void setMaxClones(string imageSet, int limit)
    {
        properties.setProperty("MaxClones." + imageSet, Math.Clamp(limit, 1, 200).ToString());
        saveProperties();
    }

    public bool canSpawn(string imageSet, bool fromBreed, bool transient)
    {
        if (transient)
            return isFlag("Transients", true);
        if (fromBreed && !isFlag("Breeding", true))
            return false;
        return manager.getCount(imageSet) < getMaxClones(imageSet);
    }

    public AppProperties getProperties() => properties;
    public PropertiesBundle getLanguageBundle() => languageBundle;
    public Manager getManager() => manager;

    public void exit()
    {
        Ai.AiCompanion.Stop();
        manager.setExitOnLastRemoved(false);
        manager.disposeAll();
        manager.stop();
        if (tray != null) tray.Visible = false;
        Application.Exit();
    }
}
