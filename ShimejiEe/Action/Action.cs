using GroupFinity.Mascot.Exception;

namespace GroupFinity.Mascot.Action;

public interface Action
{
    void init(Mascot mascot);
    bool hasNext();
    void next();
}
