using System.Numerics;

namespace MagitekStratagemServer.Trackers
{
  public interface ITrackerService : IDisposable
  {
    string Name { get; }
    bool IsTracking { get; }

    public long LastGazeTimestamp { get; }
    public Vector2 LastGazePoint { get; }

    public long LastHeadTimestamp { get; }
    public Vector3 LastHeadPosition { get; }
    public Vector3 LastHeadRotation { get; }
    
    void StartTracking(Action<ITrackerService> gazeCallback, Action<ITrackerService> headCallback);
    void StopTracking();
  }
}
