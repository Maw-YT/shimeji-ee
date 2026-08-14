using System.Drawing;

namespace GroupFinity.Mascot.Environment;

public sealed class Area
{
    public bool visible { get; set; } = true;
    public int left { get; set; }
    public int top { get; set; }
    public int right { get; set; }
    public int bottom { get; set; }
    public int dleft { get; set; }
    public int dtop { get; set; }
    public int dright { get; set; }
    public int dbottom { get; set; }

    public Wall leftBorder { get; }
    public FloorCeiling topBorder { get; }
    public Wall rightBorder { get; }
    public FloorCeiling bottomBorder { get; }

    public Area()
    {
        leftBorder = new Wall(this, false);
        topBorder = new FloorCeiling(this, false);
        rightBorder = new Wall(this, true);
        bottomBorder = new FloorCeiling(this, true);
    }

    public bool isVisible() => visible;
    public void setVisible(bool value) => visible = value;
    public int getLeft() => left;
    public void setLeft(int value) => left = value;
    public int getTop() => top;
    public void setTop(int value) => top = value;
    public int getRight() => right;
    public void setRight(int value) => right = value;
    public int getBottom() => bottom;
    public void setBottom(int value) => bottom = value;
    public int getDleft() => dleft;
    public int getDtop() => dtop;
    public int getDright() => dright;
    public int getDbottom() => dbottom;
    public Wall getLeftBorder() => leftBorder;
    public FloorCeiling getTopBorder() => topBorder;
    public Wall getRightBorder() => rightBorder;
    public FloorCeiling getBottomBorder() => bottomBorder;
    public int getWidth() => right - left;
    public int getHeight() => bottom - top;
    public int width => getWidth();
    public int height => getHeight();

    public void set(Rectangle value)
    {
        dleft = value.X - left;
        dtop = value.Y - top;
        dright = value.X + value.Width - right;
        dbottom = value.Y + value.Height - bottom;
        left = value.X;
        top = value.Y;
        right = value.X + value.Width;
        bottom = value.Y + value.Height;
    }

    public bool contains(int x, int y) => left <= x && x <= right && top <= y && y <= bottom;

    public Rectangle toRectangle() => new(left, top, right - left, bottom - top);

    public override string ToString() => $"Area [left={left}, top={top}, right={right}, bottom={bottom}]";
}
