using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Configuration;
using Microsoft.AspNetCore.SignalR.Client;

namespace MagitekStratagemPlugin
{
  public struct TrackerServiceData
  {
    public string FullName { get; set; }
    public string Name { get; set; }
  }

  public class TrackerService
  {
    public TrackerService(string fullName, string name)
    {
      FullName = fullName;
      Name = name;
    }

    public string FullName { get; set; }
    public string Name { get; set; }
    public bool IsTracking { get; set; }
    public long LastGazeTimestamp { get; set; }
    public float LastGazeX { get; set; }
    public float LastGazeY { get; set; }

    public void Process(long timestamp, float gazeX, float gazeY)
    {
      IsTracking = true;
      if (timestamp > LastGazeTimestamp)
      {
        LastGazeTimestamp = timestamp;
        LastGazeX = gazeX;
        LastGazeY = gazeY;
      }
    }

    public void Process(bool isTracking)
    {
      IsTracking = isTracking;
    }
  }

  public sealed class SignalRService
  {
    private Dictionary<string, TrackerService> trackers = new();
    private HubConnection connection;

    public HubConnectionState State => connection.State;

    public SignalRService()
    {
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

      Start();
    }

    public async void Start()
    {
      Service.PluginLog.Debug("Start");
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

    public TrackerService? GetTracker(string fullName)
    {
      Service.PluginLog.Debug($"GetTracker: {fullName}");
      if (trackers.ContainsKey(fullName))
      {
        return trackers[fullName];
      }
      else
      {
        return null;
      }
    }

    public IEnumerable<TrackerService> GetTrackers()
    {
      return trackers.Values;
    }

    public async void GetTrackerServices()
    {
      if (connection.State == HubConnectionState.Connected)
      {
        await connection.InvokeAsync("GetTrackerServices");
      }
    }

    public async void StartTracking(TrackerService service)
    {
      Service.PluginLog.Debug($"StartTracking: {service.FullName}");
      if (connection.State == HubConnectionState.Connected)
      {
        await connection.InvokeAsync("StartTracking", service.FullName);
      }
    }

    public async void StopTracking(TrackerService service)
    {
      Service.PluginLog.Debug($"StopTracking: {service.FullName}");
      if (connection.State == HubConnectionState.Connected)
      {
        await connection.InvokeAsync("StopTracking", service.FullName);
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
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
  }
}
