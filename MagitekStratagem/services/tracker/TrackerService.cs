using System.Numerics;

namespace MagitekStratagemPlugin
{
  public class TrackerService
  {
    public TrackerService(string fullName, string name)
    {
      FullName = fullName;
      Name = name;
    }

    public string Name { get; set; }
    public string FullName { get; private set; }
    public bool IsTracking { get; private set; }
    public bool PendingRequest { get; set; }
    public long LastGazeTimestamp { get; private set; }
    public Vector2 LastGazePos { get; private set; }
    public float LastGazeY { get; private set; }

    public void Process(long timestamp, float gazeX, float gazeY)
    {
      IsTracking = true;
      PendingRequest = false;
      if (timestamp > LastGazeTimestamp)
      {
        LastGazeTimestamp = timestamp;
        LastGazePos = new Vector2(gazeX, gazeY);
      }
    }

    public void Process(bool isTracking)
    {
      PendingRequest = false;
      IsTracking = isTracking;
    }
  }
}
