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

    public long LastHeadTimestamp { get; private set; }
    public Vector3 LastHeadPosition { get; private set; }
    public Vector3 LastHeadRotation { get; private set; }

    public void ProcessGaze(long timestamp, float gazeX, float gazeY)
    {
      IsTracking = true;
      PendingRequest = false;
      if (timestamp > LastGazeTimestamp)
      {
        LastGazeTimestamp = timestamp;
        LastGazePos = new Vector2(gazeX, gazeY);
      }
    }

    public void ProcessHeadPose(long timestamp, float posX, float posY, float posZ, float pitch, float yaw, float roll)
    {
      IsTracking = true;
      PendingRequest = false;
      if (timestamp > LastHeadTimestamp)
      {
        LastHeadTimestamp = timestamp;
        LastHeadPosition = new Vector3(posX, posY, posZ);
        LastHeadRotation = new Vector3(pitch, yaw, roll);
      }
    }

    public void Process(bool isTracking)
    {
      PendingRequest = false;
      IsTracking = isTracking;
    }
  }
}
