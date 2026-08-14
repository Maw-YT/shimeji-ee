using GroupFinity.Mascot.Image;

namespace GroupFinity.Mascot.Animation;

public sealed class Pose
{
    private readonly string? image;
    private readonly string? rightImage;
    private readonly int dx;
    private readonly int dy;
    private readonly int duration;
    private readonly string? sound;

    public Pose(string? image, string? rightImage, int dx, int dy, int duration, string? sound)
    {
        this.image = image;
        this.rightImage = rightImage;
        this.dx = dx;
        this.dy = dy;
        this.duration = duration;
        this.sound = sound;
    }

    public override string ToString() => $"Pose ({image},{dx},{dy},{duration}, {sound})";

    public void next(Mascot mascot)
    {
        mascot.anchor = new ScriptPoint(
            mascot.anchor.x + (mascot.lookRight ? -dx : dx),
            mascot.anchor.y + dy);
        mascot.setImage(ImagePairs.getImage(getImageName(), mascot.lookRight));
        mascot.setSound(getSoundName());
    }

    public int getDuration() => duration;

    public string getImageName() => (image ?? "") + (rightImage ?? "");

    public int getDx() => dx;
    public int getDy() => dy;
    public string? getSoundName() => sound;
}
