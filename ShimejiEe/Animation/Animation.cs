using GroupFinity.Mascot.Exception;
using GroupFinity.Mascot.Hotspot;
using GroupFinity.Mascot.Script;

namespace GroupFinity.Mascot.Animation;

public sealed class Animation
{
    private readonly Variable condition;
    private readonly Pose[] poses;
    private readonly Hotspot.Hotspot[] hotspots;
    private readonly bool turn;

    public Animation(Variable condition, Pose[] poses, Hotspot.Hotspot[] hotspots, bool turn)
    {
        if (poses.Length == 0)
            throw new ArgumentException("poses.length==0");
        this.condition = condition;
        this.poses = poses;
        this.hotspots = hotspots;
        this.turn = turn;
    }

    public bool isEffective(VariableMap variables)
    {
        try
        {
            var result = condition.get(variables);
            return result is bool b ? b : result is not null && Convert.ToBoolean(result);
        }
        catch (VariableException)
        {
            return false;
        }
    }

    public void init() => condition.init();
    public void initFrame() => condition.initFrame();
    public void next(Mascot mascot, int time) => getPoseAt(time).next(mascot);

    public Pose getPoseAt(int time)
    {
        time %= getDuration();
        foreach (var pose in poses)
        {
            time -= pose.getDuration();
            if (time < 0)
                return pose;
        }
        return poses[0];
    }

    public int getDuration()
    {
        var duration = 0;
        foreach (var pose in poses)
            duration += pose.getDuration();
        return duration;
    }

    public Hotspot.Hotspot[] getHotspots() => hotspots;
    public bool isTurn() => turn;
}
