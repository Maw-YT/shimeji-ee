namespace GroupFinity.Mascot.Exception;

public class VariableException : System.Exception
{
    public VariableException(string message) : base(message) { }
    public VariableException(string message, System.Exception inner) : base(message, inner) { }
}

public class LostGroundException : System.Exception { }

public class ConfigurationException : System.Exception
{
    public ConfigurationException(string message) : base(message) { }
    public ConfigurationException(string message, System.Exception inner) : base(message, inner) { }
}

public class CantBeAliveException : System.Exception
{
    public CantBeAliveException(string message) : base(message) { }
    public CantBeAliveException(string message, System.Exception inner) : base(message, inner) { }
}

public class BehaviorInstantiationException : System.Exception
{
    public BehaviorInstantiationException(string message) : base(message) { }
    public BehaviorInstantiationException(string message, System.Exception inner) : base(message, inner) { }
}

public class AnimationInstantiationException : System.Exception
{
    public AnimationInstantiationException(string message, System.Exception inner) : base(message, inner) { }
}

public class ActionInstantiationException : System.Exception
{
    public ActionInstantiationException(string message) : base(message) { }
    public ActionInstantiationException(string message, System.Exception inner) : base(message, inner) { }
}
