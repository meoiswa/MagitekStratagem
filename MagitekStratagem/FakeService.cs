using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using Tobii.GameIntegration.Net;

namespace MagitekStratagemPlugin
{
  public sealed class FakeService : ITrackerService
  {
    public bool IsTracking { get; private set; }
    public long LastGazeTimeStamp { get; private set; }
    public float LastGazeX { get; private set; }
    public float LastGazeY { get; private set; }
    public Vector2 LastCursorPos { get; private set; } = Vector2.Zero;
    public Vector2 DisplaySize { get; private set; } = Vector2.Zero;

    public FakeService()
    {
      LastGazeX = 0f;
      LastGazeY = 0f;
      LastGazeTimeStamp = DateTime.Now.Ticks;
    }

    public void StartTrackingWindow(nint windowHandle)
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
      LastGazeTimeStamp = DateTime.Now.Ticks;
    }

    public void Shutdown()
    {
      StopTracking();
    }

    public void Draw()
    {
      LastCursorPos = ImGui.GetMousePos();
      DisplaySize = ImGui.GetIO().DisplaySize;
    }
  }
}
