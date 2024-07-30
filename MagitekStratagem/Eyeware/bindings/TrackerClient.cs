using System;

namespace MagitekStratagemPlugin.Eyeware
{
  public class TrackerClient : IDisposable
  {
    public const string Library = "tracker_client.dll";
    public const int DefaultBaseCommunicationPort = 12010;
    public const int DefaultNetworkTimeoutsInMs = 2000;

    private IntPtr _impl;

    public TrackerClient(
        Action<NetworkError>? networkErrorHandler = null,
        int networkConnectionTimeoutMs = DefaultNetworkTimeoutsInMs,
        int trackingInfoNetworkTimeoutMs = DefaultNetworkTimeoutsInMs,
        int baseCommunicationPort = DefaultBaseCommunicationPort,
        string hostname = "127.0.0.1")
    {
      _impl = create_tracker_instance(
          hostname,
          baseCommunicationPort,
          networkConnectionTimeoutMs,
          trackingInfoNetworkTimeoutMs,
          IntPtr.Zero);
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (_impl != IntPtr.Zero)
      {
        release_tracker_instance(_impl);
        _impl = IntPtr.Zero;
      }
    }

    ~TrackerClient()
    {
      Dispose(false);
    }

    public ScreenGazeInfo GetScreenGazeInfo()
    {
      return get_screen_gaze_info(_impl);
    }

    public HeadPoseInfo GetHeadPoseInfo()
    {
      return get_head_pose_info(_impl);
    }

    public bool Connected()
    {
      return connected(_impl);
    }


    [System.Runtime.InteropServices.DllImport(Library)]
    private static extern IntPtr create_tracker_instance(
        string hostname,
        int baseCommunicationPort,
        int networkConnectionTimeoutMs,
        int trackingInfoNetworkTimeoutMs,
        IntPtr networkErrorHandler);

    [System.Runtime.InteropServices.DllImport(Library)]
    private static extern void release_tracker_instance(IntPtr instance);

    [System.Runtime.InteropServices.DllImport(Library)]
    private static extern ScreenGazeInfo get_screen_gaze_info(IntPtr instance);

    [System.Runtime.InteropServices.DllImport(Library)]
    private static extern HeadPoseInfo get_head_pose_info(IntPtr instance);

    [System.Runtime.InteropServices.DllImport(Library)]
    private static extern bool connected(IntPtr instance);

  }
}
