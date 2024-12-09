namespace MagitekStratagemServer.Trackers.Tobii.Bindings;

public sealed class Point
{
    public float X { get; set; }
    public float Y { get; set; }

    public Point(float x, float y)
    {
        X = x;
        Y = y;
    }

    internal Point()
    {
    }
}
