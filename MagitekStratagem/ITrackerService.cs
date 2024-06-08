using System.Collections;
using System.Collections.Generic;

namespace MagitekStratagemPlugin
{
  public interface ITrackerService
  {
    bool IsTracking { get; }
    long LastGazeTimeStamp { get; }
    float LastGazeX { get; }
    float LastGazeY { get; }
    float LastRawGazeX { get; }
    float LastRawGazeY { get; }
    List<CalibrationPoint> CalibrationPoints { get; }
    bool UseCalibration { get; set; }
    void StartTrackingWindow(nint windowHandle);
    void StopTracking();
    void Update();
    void Shutdown();
    void AddCalibrationPoint(float x, float y);
    virtual void Draw() { }
  }
}
