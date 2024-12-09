namespace MagitekStratagemServer.Trackers.Tobii.Bindings
{
  public class TobiiGameHubNotFoundException : Exception
  {
    public TobiiGameHubNotFoundException() : base("Tobii GameHub not found. Please install Tobii Game Hub and ensure it is running.") { }
  }

  public class TobiiGameIntegrationNotFoundException : Exception
  {
    public TobiiGameIntegrationNotFoundException() : base("Something went wrong locating the Tobii Game Integration DLL.") { }
  }

  public class TrackerServiceNotFoundException : Exception
  {
    public TrackerServiceNotFoundException() : base("Something went wrong initializing a Tracker Service.") { }
  }
}
