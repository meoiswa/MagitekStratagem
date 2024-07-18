using System;

namespace MagitekStratagemPlugin
{
  public interface ITrackerService : IDisposable
  {
    bool IsTracking { get; }
    long LastGazeTimestamp { get; }
    float LastGazeX { get; }
    float LastGazeY { get; }
    void StartTracking();
    void StopTracking();
    void Update();
    virtual void Draw() { }
  }
}
