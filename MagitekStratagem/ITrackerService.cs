using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using Tobii.GameIntegration.Net;

namespace MagitekStratagemPlugin
{
  public interface ITrackerService
  {
    bool IsTracking { get; }
    long LastGazeTimeStamp { get; }
    float LastGazeX { get; }
    float LastGazeY { get; }
    void StartTrackingWindow(nint windowHandle);
    void StopTracking();
    void Update();
    void Shutdown();
    virtual void Draw() { }
  }
}
