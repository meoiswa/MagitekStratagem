using System;
using Tobii2;

namespace MagitekStratagemPlugin
{
  public sealed class Point
  {
    public float X { get; set; }
    public float Y { get; set; }

    public Point(float x, float y)
    {
      X = x;
      Y = y;
    }

    internal Point()
    {
    }
  }

  public sealed class TobiiService : ITrackerService
  {
    private readonly Api api;
    private readonly Device device;

    public bool IsTracking { get; private set; }
    public long LastGazeTimestamp { get; private set; }
    public float LastGazeX { get; private set; }
    public float LastGazeY { get; private set; }

    public TobiiService()
    {
      var version = Tobii2.StreamEngine.GetApiVersion();
      Service.PluginLog.Verbose($"Tobii Stream Engine API Version: {version.major}.{version.minor}.{version.revision}.{version.build}");

      api = Tobii2.StreamEngine.CreateApi();

      var urls = api.EnumerateDeviceUrls();

      foreach (var url in urls)
      {
        Service.PluginLog.Verbose($"Tracker: {url}");
      }

      device = api.CreateDevice(urls[0]);

      Service.PluginLog.Verbose(device.ToString());
    }

    public void StartTracking()
    {
      this.IsTracking = true;
      device.Subscribe();
    }

    public void StopTracking()
    {
      this.IsTracking = false;
      device.Unsubscribe();
    }

    public void Update()
    {
      if (device.GazeTimestamp > LastGazeTimestamp)
      {
        // TODO: Map coordinates using window rect
        LastGazeX = device.GazeX * 2 - 1;
        LastGazeY = -(device.GazeY * 2 - 1);
        LastGazeTimestamp = device.GazeTimestamp;
      }
    }

    void Dispose(bool disposing)
    {
      if (disposing)
      {
        device?.Dispose();
        api?.Dispose();
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
  }
}
