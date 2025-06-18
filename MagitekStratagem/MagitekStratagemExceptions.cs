namespace MagitekStratagemPlugin
{
  public abstract class MagitekStratagemException : Exception
  {
    protected MagitekStratagemException(string message) : base(message) { }
  }

  public class TobiiGameHubNotFoundException : MagitekStratagemException
  {
    public TobiiGameHubNotFoundException() : base("Tobii GameHub not found. Please install Tobii Game Hub and ensure it is running.") { }
  }

  public class TobiiGameIntegrationNotFoundException : MagitekStratagemException
  {
    public TobiiGameIntegrationNotFoundException() : base("Something went wrong locating the Tobii Game Integration DLL.") { }
  }

  public class TrackerServiceNotFoundException : MagitekStratagemException
  {
    public TrackerServiceNotFoundException() : base("Something went wrong initializing a Tracker Service.") { }
  }
}
