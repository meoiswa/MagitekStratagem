namespace MagitekStratagemServer.Trackers.Tobii
{
  public class TobiiException : Exception
  {
    public TobiiException(string message) : base(message) { }
  }

  public class TobiiGameHubNotFoundException : TobiiException
  {
    public TobiiGameHubNotFoundException() : base("Tobii GameHub not found. Please install Tobii Game Hub and ensure it is running.") { }
  }

  public class TobiiGameIntegrationNotFoundException : TobiiException
  {
    public TobiiGameIntegrationNotFoundException() : base("Something went wrong locating the Tobii Game Integration DLL.") { }
  }

  public class TrackerServiceNotFoundException : TobiiException
  {
    public TrackerServiceNotFoundException() : base("Something went wrong initializing a Tracker Service.") { }
  }
}
