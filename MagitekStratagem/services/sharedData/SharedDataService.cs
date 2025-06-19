using Dalamud.Plugin;

namespace MagitekStratagemPlugin
{
  public sealed class SharedDataService : IDisposable
  {
    private readonly float[] trackerData;
    private IDalamudPluginInterface PluginInterface { get; init; }

    public SharedDataService(IDalamudPluginInterface pluginInterface)
    {
      PluginInterface = pluginInterface;

      trackerData = pluginInterface.GetOrCreateData("MagitekStratagemPlugin.TrackerData", () => new float[10]);
    }

    public void Dispose()
    {
      PluginInterface.RelinquishData("MagitekStratagemPlugin.TrackerData");
    }

    public void Update(TrackerService tracker)
    {
      if (tracker == null)
      {
        return;
      }

      // Reminder: This plugin is also a web server and it serves a test website at localhost:44244 which can be used to understand the data format.
      // Additionally, Opentrack can be used to emulate tracking with all sorts of devices, should the developer of an integration not have access
      // to an actual Eye Tracker or Head Tracker.

      if (tracker.LastGazeTimestamp > trackerData[0])
      {
        trackerData[0] = tracker.LastGazeTimestamp;
        // Negative to Positive, relative to screen, 
        // -1 and 1 are the edges of the screen,
        // but the gaze position can "spill over" the edges.
        trackerData[1] = tracker.LastGazePos.X; // Negative = left, Positive = right
        trackerData[2] = tracker.LastGazePos.Y; // Negative = down, Positive = up
      }

      if (tracker.LastHeadTimestamp > trackerData[3])
      {
        trackerData[3] = tracker.LastHeadTimestamp;

        // The head position is tracked such that the right hand rule applies to the mirror image of the user.
        // Because perspective can be confusing, position and rotation are explained "from the user's point of view".

        // Position units and center are dependent on tracker / calibration.
        // "Live" Calibration is suggested, see implementation in `site.js`.
        trackerData[4] = tracker.LastHeadPosition.X; // Negative = user is shifted to their left, Positive = user is shifted to their right.
        trackerData[5] = tracker.LastHeadPosition.Y; // Negative = user is shifted down, Positive = user is shifted up.
        trackerData[6] = tracker.LastHeadPosition.Z; // Distance from screen. Smaller = closer, Bigger = farther.

        // Rotation is in degrees, standardized
        // Positive values are clockwise rotation of their respective axis, as per right hand rule, for the mirror image of the user.
        trackerData[7] = tracker.LastHeadRotation.X; // Negative = user is looking down, Positive = user is looking up.
        trackerData[8] = tracker.LastHeadRotation.Y; // Negative = user is looking to their right, Positive = user is looking to their left.
        trackerData[9] = tracker.LastHeadRotation.Z; // Negative = user is tilting their head anticlockwise, Positive = user is tilting their head clockwise.
      }
    }
  }
}
