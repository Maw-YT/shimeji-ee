using System.Xml;
using GroupFinity.Mascot.Action;
using GroupFinity.Mascot.Animation;
using GroupFinity.Mascot.Exception;
using GroupFinity.Mascot.Hotspot;
using GroupFinity.Mascot.I18n;
using GroupFinity.Mascot.Image;
using GroupFinity.Mascot.Script;
using GroupFinity.Mascot.Sound;

namespace GroupFinity.Mascot.Config;

public sealed class Entry
{
    private readonly XmlElement element;
    private Dictionary<string, string>? attributes;
    private List<Entry>? children;
    private readonly Dictionary<string, List<Entry>> selected = new();

    public Entry(XmlElement element) => this.element = element;

    public string getName() => element.LocalName;
    public string getText() => element.InnerText;

    public Dictionary<string, string> getAttributes()
    {
        if (attributes != null) return attributes;
        attributes = new Dictionary<string, string>();
        foreach (XmlAttribute attr in element.Attributes)
            attributes[attr.LocalName] = attr.Value;
        return attributes;
    }

    public string? getAttribute(string attributeName)
        => element.HasAttribute(attributeName) ? element.GetAttribute(attributeName) : null;

    public bool hasChild(string tagName) => getChildren().Any(c => c.getName() == tagName);

    public List<Entry> selectChildren(string tagName)
    {
        if (selected.TryGetValue(tagName, out var cached))
            return cached;
        var list = getChildren().Where(c => c.getName() == tagName).ToList();
        selected[tagName] = list;
        return list;
    }

    public List<Entry> getChildren()
    {
        if (children != null) return children;
        children = new List<Entry>();
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child is XmlElement el)
                children.Add(new Entry(el));
        }
        return children;
    }
}

public interface IActionBuilder
{
    void validate();
    Action.Action buildAction(Dictionary<string, string> paramsMap);
}

public sealed class ActionRef : IActionBuilder
{
    private readonly Configuration configuration;
    private readonly string name;
    private readonly Dictionary<string, string> paramsMap = new();

    public ActionRef(Configuration configuration, Entry refNode)
    {
        this.configuration = configuration;
        name = refNode.getAttribute(configuration.getSchema().getString("Name")) ?? "";
        foreach (var kv in refNode.getAttributes())
            paramsMap[kv.Key] = kv.Value;
    }

    public override string ToString() => "Action(" + name + ")";

    public void validate()
    {
        if (!configuration.getActionBuilders().ContainsKey(name))
            throw new ConfigurationException(Main.getInstance().getLanguageBundle().getString("NoBehaviourFoundErrorMessage") + "(" + this + ")");
    }

    public Action.Action buildAction(Dictionary<string, string> paramsIn)
    {
        var newParams = new Dictionary<string, string>(paramsIn);
        foreach (var kv in paramsMap)
            newParams[kv.Key] = kv.Value;
        return configuration.buildAction(name, newParams);
    }
}

public sealed class AnimationBuilder
{
    private readonly string condition;
    private readonly string imageSet;
    private readonly List<Pose> poses = new();
    private readonly List<Hotspot.Hotspot> hotspots = new();
    private readonly PropertiesBundle schema;
    private readonly string turn;

    public AnimationBuilder(PropertiesBundle schema, Entry animationNode, string imageSet)
    {
        this.imageSet = imageSet;
        this.schema = schema;
        condition = animationNode.getAttribute(schema.getString("Condition")) ?? "true";
        turn = animationNode.getAttribute(schema.getString("IsTurn")) ?? "false";
        foreach (var frameNode in animationNode.selectChildren(schema.getString("Pose")))
        {
            var pose = loadPose(frameNode);
            if (pose != null)
                poses.Add(pose);
        }
        foreach (var frameNode in animationNode.selectChildren(schema.getString("Hotspot")))
            hotspots.Add(loadHotspot(frameNode));
    }

    private Pose? loadPose(Entry frameNode)
    {
        var imageAttr = frameNode.getAttribute(schema.getString("Image"));
        var imageRightAttr = frameNode.getAttribute(schema.getString("ImageRight"));
        var imageText = imageAttr != null ? Path.GetFullPath(Path.Combine(AppPaths.Root, "img", imageSet, imageAttr.TrimStart('/', '\\'))) : null;
        string? imageRightText = null;
        if (imageRightAttr != null)
            imageRightText = Path.GetFullPath(Path.Combine(AppPaths.Root, "img", imageSet + imageRightAttr));

        var anchorText = frameNode.getAttribute(schema.getString("ImageAnchor"));
        var moveText = frameNode.getAttribute(schema.getString("Velocity")) ?? "0,0";
        var durationText = frameNode.getAttribute(schema.getString("Duration")) ?? "1";
        var soundText = frameNode.getAttribute(schema.getString("Sound"));
        var volumeText = frameNode.getAttribute(schema.getString("Volume")) ?? "0";
        var opacity = double.Parse(Main.getInstance().getProperties().getProperty("Opacity", "1.0"), System.Globalization.CultureInfo.InvariantCulture);
        var scaling = double.Parse(Main.getInstance().getProperties().getProperty("Scaling", "1.0"), System.Globalization.CultureInfo.InvariantCulture);
        var filterText = Main.getInstance().getProperties().getProperty("Filter", "false");
        var filter = ImagePairLoader.Filter.NEAREST_NEIGHBOUR;
        if (filterText.Equals("true", StringComparison.OrdinalIgnoreCase) || filterText.Equals("hqx", StringComparison.OrdinalIgnoreCase))
            filter = ImagePairLoader.Filter.HQX;
        else if (filterText.Equals("bicubic", StringComparison.OrdinalIgnoreCase))
            filter = ImagePairLoader.Filter.BICUBIC;

        if (imageText != null)
        {
            if (!File.Exists(imageText))
            {
                Log.Warning("Missing image, skipping pose: " + imageText);
                return null;
            }
            if (imageRightText != null && !File.Exists(imageRightText))
            {
                Log.Warning("Missing right image, using mirrored left: " + imageRightText);
                imageRightText = null;
            }
            var coords = (anchorText ?? "0,0").Split(',');
            var anchor = new System.Drawing.Point(int.Parse(coords[0]), int.Parse(coords[1]));
            try
            {
                ImagePairLoader.load(imageText, imageRightText, anchor, scaling, filter, opacity);
            }
            catch (System.Exception e)
            {
                Log.Warning("Failed to load image, skipping pose: " + imageText, e);
                return null;
            }
        }

        var moveCoordinates = moveText.Split(',');
        var moveX = int.Parse(moveCoordinates[0]);
        var moveY = int.Parse(moveCoordinates[1]);
        moveX = Math.Abs(moveX) > 0 && Math.Abs(moveX * scaling) < 1 ? (moveX > 0 ? 1 : -1) : (int)Math.Round(moveX * scaling);
        moveY = Math.Abs(moveY) > 0 && Math.Abs(moveY * scaling) < 1 ? (moveY > 0 ? 1 : -1) : (int)Math.Round(moveY * scaling);
        var duration = int.Parse(durationText);

        if (soundText != null)
        {
            var candidates = new[]
            {
                Path.Combine(AppPaths.Root, "sound", soundText),
                Path.Combine(AppPaths.Root, "sound", imageSet, soundText),
                Path.Combine(AppPaths.Root, "img", imageSet, "sound", soundText)
            };
            soundText = candidates.FirstOrDefault(File.Exists) ?? candidates[^1];
            SoundLoader.load(soundText, float.Parse(volumeText, System.Globalization.CultureInfo.InvariantCulture));
            soundText += float.Parse(volumeText, System.Globalization.CultureInfo.InvariantCulture);
        }

        return new Pose(imageText, imageRightText, moveX, moveY, duration, soundText);
    }

    private Hotspot.Hotspot loadHotspot(Entry frameNode)
    {
        var shapeText = frameNode.getAttribute(schema.getString("Shape")) ?? "Rectangle";
        var originText = frameNode.getAttribute(schema.getString("Origin")) ?? "0,0";
        var sizeText = frameNode.getAttribute(schema.getString("Size")) ?? "0,0";
        var behaviourText = frameNode.getAttribute(schema.getString("Behaviour")) ?? "";
        var scaling = double.Parse(Main.getInstance().getProperties().getProperty("Scaling", "1.0"), System.Globalization.CultureInfo.InvariantCulture);
        var originCoordinates = originText.Split(',');
        var sizeCoordinates = sizeText.Split(',');
        var originX = (int)Math.Round(int.Parse(originCoordinates[0]) * scaling);
        var originY = (int)Math.Round(int.Parse(originCoordinates[1]) * scaling);
        var w = (int)Math.Round(int.Parse(sizeCoordinates[0]) * scaling);
        var h = (int)Math.Round(int.Parse(sizeCoordinates[1]) * scaling);
        var rect = new System.Drawing.RectangleF(originX, originY, w, h);
        return new Hotspot.Hotspot(behaviourText, rect, shapeText.Equals("Ellipse", StringComparison.OrdinalIgnoreCase));
    }

    public Animation.Animation buildAnimation()
        => new(Variable.parse(condition), poses.ToArray(), hotspots.ToArray(), bool.Parse(turn));

    public bool hasPoses() => poses.Count > 0;
}

public sealed class ActionBuilder : IActionBuilder
{
    private readonly string type;
    private readonly string name;
    private readonly string? className;
    private readonly Dictionary<string, string> paramsMap = new();
    private readonly List<AnimationBuilder> animationBuilders = new();
    private readonly List<IActionBuilder> actionRefs = new();
    private readonly PropertiesBundle schema;

    public ActionBuilder(Configuration configuration, Entry actionNode, string imageSet)
    {
        schema = configuration.getSchema();
        name = actionNode.getAttribute(schema.getString("Name")) ?? "";
        type = actionNode.getAttribute(schema.getString("Type")) ?? "";
        className = actionNode.getAttribute(schema.getString("Class"));
        foreach (var kv in actionNode.getAttributes())
            paramsMap[kv.Key] = kv.Value;
        foreach (var node in actionNode.selectChildren(schema.getString("Animation")))
        {
            var animation = new AnimationBuilder(schema, node, imageSet);
            if (animation.hasPoses())
                animationBuilders.Add(animation);
        }
        foreach (var node in actionNode.getChildren())
        {
            if (node.getName() == schema.getString("ActionReference"))
                actionRefs.Add(new ActionRef(configuration, node));
            else if (node.getName() == schema.getString("Action"))
                actionRefs.Add(new ActionBuilder(configuration, node, imageSet));
        }
    }

    public override string ToString() => $"Action({name},{type},{className})";
    public string getName() => name;

    public void validate()
    {
        foreach (var r in actionRefs) r.validate();
    }

    public Action.Action buildAction(Dictionary<string, string> extra)
    {
        var variables = new VariableMap();
        foreach (var param in paramsMap)
            variables.put(param.Key, Variable.parse(param.Value));
        foreach (var param in extra)
            variables.put(param.Key, Variable.parse(param.Value));

        var animations = animationBuilders.Select(a => a.buildAnimation()).ToList();
        var actions = actionRefs.Select(r => r.buildAction(new Dictionary<string, string>())).ToArray();

        if (type == schema.getString("Embedded"))
            return ActionFactory.Create(className ?? "", schema, animations, variables);
        if (type == schema.getString("Move"))
            return new Move(schema, animations, variables);
        if (type == schema.getString("Stay"))
            return new Stay(schema, animations, variables);
        if (type == schema.getString("Animate"))
            return new Animate(schema, animations, variables);
        if (type == schema.getString("Sequence"))
            return new Sequence(schema, variables, actions);
        if (type == schema.getString("Select"))
            return new Select(schema, variables, actions);
        throw new ActionInstantiationException(Main.getInstance().getLanguageBundle().getString("UnknownActionTypeErrorMessage") + "(" + this + ")");
    }
}

public static class ActionFactory
{
    public static Action.Action Create(string javaClass, PropertiesBundle schema, List<Animation.Animation> animations, VariableMap variables)
    {
        var simple = javaClass.Contains('.') ? javaClass[(javaClass.LastIndexOf('.') + 1)..] : javaClass;
        return simple switch
        {
            "Look" => new Look(schema, variables),
            "Offset" => new Offset(schema, variables),
            "Jump" => new Jump(schema, animations, variables),
            "Fall" => new Fall(schema, animations, variables),
            "Dragged" => new Dragged(schema, animations, variables),
            "Regist" => new Regist(schema, animations, variables),
            "Breed" => new Breed(schema, animations, variables),
            "BreedJump" => new BreedJump(schema, animations, variables),
            "BreedMove" => new BreedMove(schema, animations, variables),
            "Transform" => new Transform(schema, animations, variables),
            "ThrowIE" => new ThrowIE(schema, animations, variables),
            "WalkWithIE" => new WalkWithIE(schema, animations, variables),
            "FallWithIE" => new FallWithIE(schema, animations, variables),
            "Turn" => new Turn(schema, animations, variables),
            "Mute" => new Mute(schema, variables),
            "SelfDestruct" => new SelfDestruct(schema, variables),
            "Interact" => new Interact(schema, animations, variables),
            "ScanMove" => new ScanMove(schema, animations, variables),
            "ScanJump" => new ScanJump(schema, animations, variables),
            "ScanInteract" => new ScanInteract(schema, animations, variables),
            "Broadcast" => new Broadcast(schema, animations, variables),
            "BroadcastStay" => new BroadcastStay(schema, animations, variables),
            "BroadcastMove" => new BroadcastMove(schema, animations, variables),
            "BroadcastJump" => new BroadcastJump(schema, animations, variables),
            "ComplexMove" => new ComplexMove(schema, animations, variables),
            "ComplexJump" => new ComplexJump(schema, animations, variables),
            "MoveWithTurn" => new MoveWithTurn(schema, animations, variables),
            "Animate" => new Animate(schema, animations, variables),
            "Stay" => new Stay(schema, animations, variables),
            "Move" => new Move(schema, animations, variables),
            _ => throw new ActionInstantiationException(Main.getInstance().getLanguageBundle().getString("ClassNotFoundErrorMessage") + "(" + javaClass + ")")
        };
    }
}
