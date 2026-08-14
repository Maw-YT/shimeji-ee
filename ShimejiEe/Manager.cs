using GroupFinity.Mascot.Config;
using GroupFinity.Mascot.Exception;

namespace GroupFinity.Mascot;

public sealed class Manager
{
    public const int TICK_INTERVAL = 40;
    private readonly List<Mascot> mascots = new();
    private readonly HashSet<Mascot> added = new();
    private readonly HashSet<Mascot> removed = new();
    private bool exitOnLastRemoved = true;
    private Thread? thread;
    private volatile bool running;

    public void setExitOnLastRemoved(bool value) => exitOnLastRemoved = value;
    public bool isExitOnLastRemoved() => exitOnLastRemoved;

    public void start()
    {
        if (thread != null && thread.IsAlive) return;
        running = true;
        thread = new Thread(Run) { IsBackground = false };
        thread.Start();
    }

    public void stop()
    {
        running = false;
        thread?.Interrupt();
        try { thread?.Join(1000); } catch { }
    }

    private void Run()
    {
        var prev = System.Environment.TickCount64;
        try
        {
            while (running)
            {
                while (running)
                {
                    var cur = System.Environment.TickCount64;
                    if (cur - prev >= TICK_INTERVAL)
                    {
                        if (cur > prev + TICK_INTERVAL * 2) prev = cur;
                        else prev += TICK_INTERVAL;
                        break;
                    }
                    Thread.Sleep(Math.Max(1, (int)(TICK_INTERVAL - (cur - prev))));
                }
                if (running) tick();
            }
        }
        catch (ThreadInterruptedException) { }
        catch (System.Exception e)
        {
            Log.Severe("Animation loop failed", e);
        }
    }

    private void tick()
    {
        NativeFactory.getInstance().getEnvironment().tick();
        List<Mascot> snapshot;
        lock (mascots)
        {
            lock (added)
            {
                foreach (var mascot in added) mascots.Add(mascot);
                added.Clear();
                foreach (var mascot in removed) mascots.Remove(mascot);
                removed.Clear();
            }
            snapshot = mascots.ToList();
        }
        foreach (var mascot in snapshot)
        {
            try { mascot.tick(); }
            catch (System.Exception e) { Log.Severe("Mascot tick failed", e); }
        }
        foreach (var mascot in snapshot)
        {
            try { mascot.apply(); }
            catch (System.Exception e) { Log.Severe("Mascot apply failed", e); }
        }
        if (exitOnLastRemoved && getCount() == 0)
            UiSync.Post(() => Main.getInstance().exit());
    }

    public void add(Mascot mascot)
    {
        lock (added)
        {
            added.Add(mascot);
            removed.Remove(mascot);
        }
        mascot.setManager(this);
    }

    public void remove(Mascot mascot)
    {
        lock (added)
        {
            added.Remove(mascot);
            removed.Add(mascot);
        }
        mascot.setManager(null);
    }

    public void forceBehaviorAll(string name)
    {
        List<Mascot> snapshot;
        lock (mascots)
            snapshot = mascots.ToList();
        foreach (var mascot in snapshot)
            mascot.queueBehavior(name);
    }

    public void setBehaviorAll(string name)
    {
        lock (mascots)
        {
            foreach (var mascot in mascots.ToList())
            {
                try
                {
                    var configuration = Main.getInstance().getConfiguration(mascot.imageSet);
                    mascot.setBehavior(configuration.buildBehavior(configuration.getSchema().getString(name), mascot));
                }
                catch (System.Exception e)
                {
                    Log.Severe("Failed to set behavior", e);
                    Main.showError(Main.getInstance().getLanguageBundle().getString("FailedSetBehaviourErrorMessage"), e);
                    mascot.dispose();
                }
            }
        }
    }

    public void setBehaviorAll(Configuration configuration, string name, string imageSet)
    {
        lock (mascots)
        {
            foreach (var mascot in mascots.ToList())
            {
                try
                {
                    if (mascot.imageSet == imageSet)
                        mascot.setBehavior(configuration.buildBehavior(configuration.getSchema().getString(name), mascot));
                }
                catch (System.Exception e)
                {
                    Log.Severe("Failed to set behavior", e);
                    Main.showError(Main.getInstance().getLanguageBundle().getString("FailedSetBehaviourErrorMessage"), e);
                    mascot.dispose();
                }
            }
        }
    }

    public void remainOne()
    {
        lock (mascots)
        {
            for (var i = mascots.Count - 1; i > 0; --i)
                mascots[i].dispose();
        }
    }

    public void remainOne(Mascot mascot)
    {
        lock (mascots)
        {
            for (var i = mascots.Count - 1; i >= 0; --i)
            {
                if (!ReferenceEquals(mascots[i], mascot))
                    mascots[i].dispose();
            }
        }
    }

    public void remainOne(string imageSet)
    {
        lock (mascots)
        {
            var isFirst = true;
            for (var i = mascots.Count - 1; i >= 0; --i)
            {
                var m = mascots[i];
                if (m.imageSet == imageSet && isFirst) isFirst = false;
                else if (m.imageSet == imageSet) m.dispose();
            }
        }
    }

    public void remainNone(string imageSet)
    {
        lock (mascots)
        {
            for (var i = mascots.Count - 1; i >= 0; --i)
            {
                if (mascots[i].imageSet == imageSet)
                    mascots[i].dispose();
            }
        }
    }

    public void togglePauseAll()
    {
        lock (mascots)
        {
            var isPaused = mascots.All(m => m.paused);
            foreach (var mascot in mascots)
                mascot.paused = !isPaused;
        }
    }

    public bool isPaused()
    {
        lock (mascots)
            return mascots.Count > 0 && mascots.All(m => m.paused);
    }

    public int getCount(string? imageSet = null)
    {
        lock (mascots)
        {
            if (imageSet == null) return mascots.Count;
            return mascots.Count(m => m.imageSet == imageSet);
        }
    }

    public WeakReference<Mascot>? getMascotWithAffordance(string affordance)
    {
        lock (mascots)
        {
            foreach (var mascot in mascots)
            {
                if (mascot.getAffordances().Contains(affordance))
                    return new WeakReference<Mascot>(mascot);
            }
        }
        return null;
    }

    public List<Mascot> snapshot()
    {
        lock (mascots)
            return mascots.ToList();
    }

    public bool hasOverlappingMascotsAtPoint(ScriptPoint anchor)
    {
        lock (mascots)
            return mascots.Count(m => m.anchor.Equals(anchor)) > 1;
    }

    public void disposeAll()
    {
        lock (mascots)
        {
            for (var i = mascots.Count - 1; i >= 0; --i)
                mascots[i].dispose();
        }
    }
}
