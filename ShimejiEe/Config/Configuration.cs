using GroupFinity.Mascot.Action;
using GroupFinity.Mascot.Behavior;
using GroupFinity.Mascot.Exception;
using GroupFinity.Mascot.I18n;
using GroupFinity.Mascot.Script;

namespace GroupFinity.Mascot.Config;

public sealed class BehaviorBuilder
{
    private readonly Configuration configuration;
    private readonly string name;
    private readonly string actionName;
    private readonly int frequency;
    private readonly List<string?> conditions;
    private readonly bool hidden;
    private readonly bool toggleable;
    private readonly bool nextAdditive;
    private readonly List<BehaviorBuilder> nextBehaviorBuilders = new();
    private readonly Dictionary<string, string> paramsMap = new();

    public BehaviorBuilder(Configuration configuration, Entry behaviorNode, List<string> conditions)
    {
        this.configuration = configuration;
        name = behaviorNode.getAttribute(configuration.getSchema().getString("Name")) ?? "";
        actionName = behaviorNode.getAttribute(configuration.getSchema().getString("Action")) ?? name;
        frequency = int.Parse(behaviorNode.getAttribute(configuration.getSchema().getString("Frequency")) ?? "0");
        hidden = bool.Parse(behaviorNode.getAttribute(configuration.getSchema().getString("Hidden")) ?? "false");
        this.conditions = new List<string?>(conditions) { behaviorNode.getAttribute(configuration.getSchema().getString("Condition")) };

        if (name == UserBehavior.BEHAVIOURNAME_FALL || name == UserBehavior.BEHAVIOURNAME_THROWN || name == UserBehavior.BEHAVIOURNAME_DRAGGED)
            toggleable = false;
        else
            toggleable = bool.Parse(behaviorNode.getAttribute(configuration.getSchema().getString("Toggleable")) ?? "true");

        foreach (var kv in behaviorNode.getAttributes())
            paramsMap[kv.Key] = kv.Value;
        paramsMap.Remove(configuration.getSchema().getString("Name"));
        paramsMap.Remove(configuration.getSchema().getString("Action"));
        paramsMap.Remove(configuration.getSchema().getString("Frequency"));
        paramsMap.Remove(configuration.getSchema().getString("Hidden"));
        paramsMap.Remove(configuration.getSchema().getString("Condition"));
        paramsMap.Remove(configuration.getSchema().getString("Toggleable"));

        var additive = true;
        foreach (var nextList in behaviorNode.selectChildren(configuration.getSchema().getString("NextBehaviourList")))
        {
            additive = bool.Parse(nextList.getAttribute(configuration.getSchema().getString("Add")) ?? "true");
            loadBehaviors(nextList, new List<string>());
        }
        nextAdditive = additive;
    }

    public override string ToString() => $"Behavior({name},{frequency},{actionName})";

    private void loadBehaviors(Entry list, List<string> conditionsIn)
    {
        foreach (var node in list.getChildren())
        {
            if (node.getName() == configuration.getSchema().getString("Condition"))
            {
                var newConditions = new List<string>(conditionsIn) { node.getAttribute(configuration.getSchema().getString("Condition")) ?? "" };
                loadBehaviors(node, newConditions);
            }
            else if (node.getName() == configuration.getSchema().getString("BehaviourReference"))
                nextBehaviorBuilders.Add(new BehaviorBuilder(configuration, node, conditionsIn));
        }
    }

    public void validate()
    {
        if (!configuration.getActionBuilders().ContainsKey(actionName))
            throw new ConfigurationException(Main.getInstance().getLanguageBundle().getString("NoActionFoundErrorMessage") + "(" + this + ")");
    }

    public Behavior.Behavior buildBehavior()
        => new UserBehavior(name, configuration.buildAction(actionName, paramsMap), configuration);

    public bool isEffective(VariableMap context)
    {
        if (frequency == 0) return false;
        foreach (var condition in conditions)
        {
            if (condition != null)
            {
                var result = Variable.parse(condition).get(context);
                if (result is not true && !Convert.ToBoolean(result ?? false))
                    return false;
            }
        }
        return true;
    }

    public string getName() => name;
    public int getFrequency() => frequency;
    public bool isHidden() => hidden;
    public bool isToggleable() => toggleable;
    public bool isNextAdditive() => nextAdditive;
    public List<BehaviorBuilder> getNextBehaviorBuilders() => nextBehaviorBuilders;
}

public sealed class Configuration
{
    private readonly Dictionary<string, string> constants = new();
    private readonly Dictionary<string, ActionBuilder> actionBuilders = new();
    private readonly Dictionary<string, BehaviorBuilder> behaviorBuilders = new();
    private readonly Dictionary<string, string> information = new();
    private PropertiesBundle? schema;

    public void load(Entry configurationNode, string imageSet)
    {
        var locale = (configurationNode.hasChild("動作リスト") || configurationNode.hasChild("行動リスト")) ? "ja-JP" : "en-US";
        schema = PropertiesBundle.GetBundle("schema", locale);

        foreach (var constant in configurationNode.selectChildren(schema.getString("Constant")))
            constants[constant.getAttribute(schema.getString("Name")) ?? ""] = constant.getAttribute(schema.getString("Value")) ?? "";

        foreach (var list in configurationNode.selectChildren(schema.getString("ActionList")))
        {
            foreach (var node in list.selectChildren(schema.getString("Action")))
            {
                var action = new ActionBuilder(this, node, imageSet);
                if (actionBuilders.ContainsKey(action.getName()))
                    throw new ConfigurationException(Main.getInstance().getLanguageBundle().getString("DuplicateActionErrorMessage") + ": " + action.getName());
                actionBuilders[action.getName()] = action;
            }
        }

        foreach (var list in configurationNode.selectChildren(schema.getString("BehaviourList")))
            loadBehaviors(list, new List<string>());

        foreach (var list in configurationNode.selectChildren(schema.getString("Information")))
            loadInformation(list);
    }

    private void loadBehaviors(Entry list, List<string> conditions)
    {
        foreach (var node in list.getChildren())
        {
            if (node.getName() == schema!.getString("Condition"))
            {
                var newConditions = new List<string>(conditions) { node.getAttribute(schema.getString("Condition")) ?? "" };
                loadBehaviors(node, newConditions);
            }
            else if (node.getName() == schema.getString("Behaviour"))
            {
                var behavior = new BehaviorBuilder(this, node, conditions);
                behaviorBuilders[behavior.getName()] = behavior;
            }
        }
    }

    private void loadInformation(Entry list)
    {
        foreach (var node in list.getChildren())
        {
            if (node.getName() == schema!.getString("Name") || node.getName() == schema.getString("PreviewImage") || node.getName() == schema.getString("SplashImage"))
                information[node.getName()] = node.getText();
        }
    }

    public Action.Action buildAction(string name, Dictionary<string, string> paramsMap)
    {
        if (!actionBuilders.TryGetValue(name, out var factory))
            throw new ActionInstantiationException(Main.getInstance().getLanguageBundle().getString("NoCorrespondingActionFoundErrorMessage") + ": " + name);
        return factory.buildAction(paramsMap);
    }

    public void validate()
    {
        foreach (var b in actionBuilders.Values) b.validate();
        foreach (var b in behaviorBuilders.Values) b.validate();
    }

    public Behavior.Behavior buildNextBehavior(string? previousName, Mascot mascot)
    {
        var context = new VariableMap();
        context.putAll(constants);
        context.put("mascot", mascot);
        var candidates = new List<BehaviorBuilder>();
        long totalFrequency = 0;
        foreach (var factory in behaviorBuilders.Values)
        {
            try
            {
                if (factory.isEffective(context) && isBehaviorEnabled(factory, mascot))
                {
                    candidates.Add(factory);
                    totalFrequency += factory.getFrequency();
                }
            }
            catch (VariableException e) { Log.Warning("frequency error", e); }
        }

        if (previousName != null && behaviorBuilders.TryGetValue(previousName, out var previous))
        {
            if (!previous.isNextAdditive())
            {
                totalFrequency = 0;
                candidates.Clear();
            }
            foreach (var factory in previous.getNextBehaviorBuilders())
            {
                try
                {
                    if (factory.isEffective(context) && isBehaviorEnabled(factory, mascot))
                    {
                        candidates.Add(factory);
                        totalFrequency += factory.getFrequency();
                    }
                }
                catch (VariableException e) { Log.Warning("frequency error", e); }
            }
        }

        if (totalFrequency == 0)
        {
            var area = bool.Parse(Main.getInstance().getProperties().getProperty("Multiscreen", "true")) ? mascot.environment.getScreen() : mascot.environment.getWorkArea();
            mascot.anchor = new ScriptPoint((int)(Random.Shared.NextDouble() * (area.getRight() - area.getLeft())) + area.getLeft(), area.getTop() - 256);
            return buildBehavior(schema!.getString(UserBehavior.BEHAVIOURNAME_FALL));
        }

        var random = Random.Shared.NextDouble() * totalFrequency;
        foreach (var factory in candidates)
        {
            random -= factory.getFrequency();
            if (random < 0)
                return factory.buildBehavior();
        }
        return candidates[^1].buildBehavior();
    }

    public Behavior.Behavior buildBehavior(string name, Mascot mascot)
    {
        if (behaviorBuilders.ContainsKey(name))
        {
            if (isBehaviorEnabled(name, mascot))
                return behaviorBuilders[name].buildBehavior();
            var area = bool.Parse(Main.getInstance().getProperties().getProperty("Multiscreen", "true")) ? mascot.environment.getScreen() : mascot.environment.getWorkArea();
            mascot.anchor = new ScriptPoint((int)(Random.Shared.NextDouble() * (area.getRight() - area.getLeft())) + area.getLeft(), area.getTop() - 256);
            return buildBehavior(schema!.getString(UserBehavior.BEHAVIOURNAME_FALL));
        }
        throw new BehaviorInstantiationException(Main.getInstance().getLanguageBundle().getString("NoBehaviourFoundErrorMessage") + " (" + name + ")");
    }

    public Behavior.Behavior buildBehavior(string name)
    {
        if (behaviorBuilders.ContainsKey(name))
            return behaviorBuilders[name].buildBehavior();
        throw new BehaviorInstantiationException(Main.getInstance().getLanguageBundle().getString("NoBehaviourFoundErrorMessage") + " (" + name + ")");
    }

    public bool isBehaviorEnabled(BehaviorBuilder builder, Mascot mascot)
        => isBehaviorEnabled(builder, mascot.imageSet);

    public bool isBehaviorEnabled(BehaviorBuilder builder, string imageSet)
    {
        if (!builder.isToggleable()) return true;
        foreach (var behaviour in Main.getInstance().getProperties().getProperty("DisabledBehaviours." + imageSet, "").Split('/'))
        {
            if (behaviour == builder.getName()) return false;
        }
        return true;
    }

    public bool isBehaviorEnabled(string name, Mascot mascot)
        => isBehaviorEnabled(name, mascot.imageSet);

    public bool isBehaviorEnabled(string name, string imageSet)
        => behaviorBuilders.ContainsKey(name) && isBehaviorEnabled(behaviorBuilders[name], imageSet);

    public bool isBehaviorHidden(string name) => behaviorBuilders.ContainsKey(name) && behaviorBuilders[name].isHidden();
    public bool isBehaviorToggleable(string name) => behaviorBuilders.ContainsKey(name) && behaviorBuilders[name].isToggleable();
    public Dictionary<string, ActionBuilder> getActionBuilders() => actionBuilders;
    public IEnumerable<string> getBehaviorNames() => behaviorBuilders.Keys;
    public bool containsInformationKey(string key) => information.ContainsKey(key);
    public string? getInformation(string key) => information.GetValueOrDefault(key);
    public PropertiesBundle getSchema() => schema!;
}
