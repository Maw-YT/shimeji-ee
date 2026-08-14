namespace GroupFinity.Mascot.Environment;

public sealed class MascotEnvironment
{
    private readonly Environment impl;
    private readonly Mascot mascot;
    private Area? currentWorkArea;

    public MascotEnvironment(Mascot mascot)
    {
        this.mascot = mascot;
        impl = NativeFactory.getInstance().getEnvironment();
        impl.init();
    }

    public Border getCeiling() => getCeiling(false);
    public Border ceiling => getCeiling();

    public Border getCeiling(bool ignoreSeparator)
    {
        if (getActiveIE().getBottomBorder().isOn(mascot.anchor))
            return getActiveIE().getBottomBorder();
        if (getWorkArea().getTopBorder().isOn(mascot.anchor))
        {
            if (!ignoreSeparator || isScreenTopBottom())
                return getWorkArea().getTopBorder();
        }
        return NotOnBorder.INSTANCE;
    }

    public Area getWorkArea() => getWorkArea(false);
    public Area workArea => getWorkArea();

    public Area getWorkArea(bool ignoreSettings)
    {
        if (currentWorkArea != null)
        {
            if (ignoreSettings || bool.Parse(Main.getInstance().getProperties().getProperty("Multiscreen", "true")))
            {
                if (currentWorkArea != impl.WorkArea && currentWorkArea.toRectangle().Contains(impl.WorkArea.toRectangle()))
                {
                    if (impl.WorkArea.contains(mascot.anchor.x, mascot.anchor.y))
                    {
                        currentWorkArea = impl.WorkArea;
                        return currentWorkArea;
                    }
                }
                if (currentWorkArea.contains(mascot.anchor.x, mascot.anchor.y))
                    return currentWorkArea;
            }
            else return currentWorkArea;
        }

        if (impl.WorkArea.contains(mascot.anchor.x, mascot.anchor.y))
        {
            currentWorkArea = impl.WorkArea;
            return currentWorkArea;
        }

        foreach (var area in impl.getScreens())
        {
            if (area.contains(mascot.anchor.x, mascot.anchor.y))
            {
                currentWorkArea = area;
                return currentWorkArea;
            }
        }

        currentWorkArea = impl.WorkArea;
        return currentWorkArea;
    }

    public Area getActiveIE()
    {
        var activeIE = impl.getActiveIE();
        if (currentWorkArea != null &&
            !bool.Parse(Main.getInstance().getProperties().getProperty("Multiscreen", "true")) &&
            !currentWorkArea.toRectangle().IntersectsWith(activeIE.toRectangle()))
            return new Area();
        return activeIE;
    }

    public Area activeIE => getActiveIE();

    public string getActiveIETitle() => impl.getActiveIETitle();

    public ComplexArea getComplexScreen() => impl.getComplexScreen();
    public ComplexArea complexScreen => getComplexScreen();

    public Location getCursor() => impl.getCursor();
    public Location cursor => getCursor();

    public Border getFloor() => getFloor(false);
    public Border floor => getFloor();

    public Border getFloor(bool ignoreSeparator)
    {
        if (getActiveIE().getTopBorder().isOn(mascot.anchor))
            return getActiveIE().getTopBorder();
        if (getWorkArea().getBottomBorder().isOn(mascot.anchor))
        {
            if (!ignoreSeparator || isScreenTopBottom())
                return getWorkArea().getBottomBorder();
        }
        return NotOnBorder.INSTANCE;
    }

    public Area getScreen() => impl.getScreen();
    public Area screen => getScreen();

    public Border getWall() => getWall(false);
    public Border wall => getWall();

    public Border getWall(bool ignoreSeparator)
    {
        if (mascot.lookRight)
        {
            if (getActiveIE().getLeftBorder().isOn(mascot.anchor))
                return getActiveIE().getLeftBorder();
            if (getWorkArea().getRightBorder().isOn(mascot.anchor))
            {
                if (!ignoreSeparator || isScreenLeftRight())
                    return getWorkArea().getRightBorder();
            }
        }
        else
        {
            if (getActiveIE().getRightBorder().isOn(mascot.anchor))
                return getActiveIE().getRightBorder();
            if (getWorkArea().getLeftBorder().isOn(mascot.anchor))
            {
                if (!ignoreSeparator || isScreenLeftRight())
                    return getWorkArea().getLeftBorder();
            }
        }
        return NotOnBorder.INSTANCE;
    }

    public void moveActiveIE(ScriptPoint point) => impl.moveActiveIE(point);
    public void restoreIE() => impl.restoreIE();
    public void refreshWorkArea() => getWorkArea(true);

    private bool isScreenTopBottom() => impl.isScreenTopBottom(mascot.anchor);
    private bool isScreenLeftRight() => impl.isScreenLeftRight(mascot.anchor);
}
