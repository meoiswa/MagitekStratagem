using System.Diagnostics;
using Microsoft.AspNetCore.SignalR.Client;

namespace MagitekStratagemPlugin
{
  public sealed class SignalRService
  {
    private Dictionary<string, TrackerService> trackers = new();
    private readonly FileInfo assemblyLocation;
    private readonly Configuration configuration;
    private HubConnection connection;
    private Process? serverProcess;

    public HubConnectionState State => connection.State;
    public TrackerService? ActiveTracker { get; private set; }

    public SignalRService(
      FileInfo assemblyLocation,
      Configuration configuration)
    {
      this.assemblyLocation = assemblyLocation;
      this.configuration = configuration;

      connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:44244/hub")
                .WithAutomaticReconnect()
                .Build();

      connection.On("TrackerUpdate", (string fullName, long timestamp, float eyeX, float eyeY) =>
      {
        GetOrCreateTracker(fullName).Process(timestamp, eyeX, eyeY);
      });

      connection.On("TrackerServices", (TrackerServiceData[] services) =>
      {
        foreach (var service in services)
        {
          Service.PluginLog.Debug($"TrackerServices: {service.FullName} {service.Name}");
          var tracker = GetOrCreateTracker(service.FullName, service.Name);
        }
      });

      connection.On("TrackingStarted", (string fullName) =>
      {
        Service.PluginLog.Debug($"TrackingStarted: {fullName}");
        GetOrCreateTracker(fullName).Process(true);
      });

      connection.On("TrackingStopped", (string fullName) =>
      {
        Service.PluginLog.Debug($"TrackingStopped: {fullName}");
        GetOrCreateTracker(fullName).Process(false);
      });

      RunServer();
      Start();
    }

    public void RunServer()
    {
      KillServer();

      var path = assemblyLocation.FullName.Replace(".dll", ".exe");
      Service.PluginLog.Info("Starting server process at " + path);
      serverProcess = new Process
      {
        StartInfo = new ProcessStartInfo
        {
          FileName = path,
          UseShellExecute = false,
          CreateNoWindow = true,
          WorkingDirectory = Path.Combine(assemblyLocation.DirectoryName!, "server"),
        }
      };
      try
      {
        serverProcess.Start();
      }
      catch (Exception ex)
      {
        Service.PluginLog.Error(ex, "Failed to start server process");
      }
    }

    public void KillServer()
    {
      if (serverProcess != null && serverProcess.HasExited == false)
      {
        try
        {
          serverProcess.Kill();
        }
        catch (Exception ex)
        {
          Service.PluginLog.Error(ex, "Failed to kill existing server process");
        }
      }

      serverProcess = null;
    }

    public async void Start()
    {
      Service.PluginLog.Debug("Start");

      if (serverProcess == null || serverProcess.HasExited)
      {
        RunServer();
      }

      if (connection.State == HubConnectionState.Disconnected)
      {
        try
        {
          await connection.StartAsync();
        }
        catch (Exception ex)
        {
          Service.PluginLog.Error(ex, "Failed to start SignalR connection");
          return;
        }
        GetTrackerServices();
      }
    }

    public void Update()
    {
      if (ActiveTracker == null || ActiveTracker.FullName != configuration.SelectedTrackerFullName)
      {
        if (ActiveTracker != null)
        {
          StopTracking(ActiveTracker);
          ActiveTracker = null;
        }

        if (configuration.SelectedTrackerFullName != string.Empty)
        {
          ActiveTracker = GetTracker(configuration.SelectedTrackerFullName);
        }
      }

      if (ActiveTracker != null && ActiveTracker.IsTracking == false)
      {
        StartTracking(ActiveTracker);
      }
    }

    public IEnumerable<TrackerService> GetTrackers()
    {
      return trackers.Values;
    }

    public async void StartTracking(TrackerService service)
    {
      Service.PluginLog.Debug($"StartTracking: {service.FullName}");
      if (connection.State == HubConnectionState.Connected && !service.IsTracking && !service.PendingRequest)
      {
        service.PendingRequest = true;
        await connection.InvokeAsync("StartTracking", service.FullName);
      }
    }

    public async void StopTracking(TrackerService service)
    {
      Service.PluginLog.Debug($"StopTracking: {service.FullName}");
      if (connection.State == HubConnectionState.Connected && service.IsTracking && !service.PendingRequest)
      {
        service.PendingRequest = true;
        await connection.InvokeAsync("StopTracking", service.FullName);
      }
    }

    private TrackerService? GetTracker(string fullName)
    {
      if (trackers.ContainsKey(fullName))
      {
        return trackers[fullName];
      }
      else
      {
        return null;
      }
    }

    private async void GetTrackerServices()
    {
      if (connection.State == HubConnectionState.Connected)
      {
        await connection.InvokeAsync("GetTrackerServices");
      }
    }

    private TrackerService GetOrCreateTracker(string fullName, string? name = null)
    {
      if (trackers.ContainsKey(fullName))
      {
        var tracker = trackers[fullName];
        if (name != null)
        {
          tracker.Name = name;
        }
        return tracker;
      }
      else
      {
        var tracker = new TrackerService(fullName, name ?? fullName);
        trackers.Add(fullName, tracker);
        return tracker;
      }
    }

    private async void Dispose(bool disposing)
    {
      if (disposing)
      {
        if (connection != null)
        {
          if (connection.State == HubConnectionState.Connected)
          {
            await connection.StopAsync();
          }
          await connection.DisposeAsync();
        }
        KillServer();
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
  }
}
