namespace MagitekStratagemServer.Trackers
{
  public interface ITrackerService : IDisposable
  {
    string Name { get; }
    bool IsTracking { get; }
    long LastGazeTimestamp { get; }
    float LastGazeX { get; }
    float LastGazeY { get; }
    void StartTracking(Action<ITrackerService> callback);
    void StopTracking();
  }
}
