using System;
using System.Numerics;
using ImGuiNET;

namespace MagitekStratagemPlugin
{
  public sealed class FakeService : ITrackerService
  {
    public bool IsTracking { get; private set; }
    public long LastGazeTimestamp { get; private set; }
    public float LastGazeX { get; private set; }
    public float LastGazeY { get; private set; }
    public Vector2 LastCursorPos { get; private set; } = Vector2.Zero;
    public Vector2 DisplaySize { get; private set; } = Vector2.Zero;

    public FakeService()
    {
      LastGazeX = 0f;
      LastGazeY = 0f;
      LastGazeTimestamp = 0;
    }

    public void StartTracking()
    {
      if (!IsTracking)
      {
        IsTracking = true;
      }
    }

    public void StopTracking()
    {
      if (IsTracking)
      {
        IsTracking = false;
      }
    }

    public void Update()
    {
      if (DisplaySize != Vector2.Zero)
      {
        // calculate the relative mouse position as a range from [-1, -1] to [1, 1]
        var pos = LastCursorPos / DisplaySize * 2 - Vector2.One;
        LastGazeX = pos.X;
        LastGazeY = -pos.Y;
      }
      LastGazeTimestamp = DateTime.Now.Ticks;
    }

    public void Draw()
    {
      LastCursorPos = ImGui.GetMousePos();
      DisplaySize = ImGui.GetIO().DisplaySize;
    }

    private void Dispose(bool disposing)
    {
      if (disposing)
      {
        StopTracking();
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
  }
}
