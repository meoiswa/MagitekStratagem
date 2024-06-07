using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace MagitekStratagemPlugin
{
  // It is good to have this be disposable in general, in case you ever need it
  // to do any cleanup
  public unsafe class MagitekStratagemOverlay : Window, IDisposable
  {
    private readonly MagitekStratagemPlugin plugin;

    public MagitekStratagemOverlay(MagitekStratagemPlugin plugin)
      : base(
        "MagitekStratagem##Overlay",
        ImGuiWindowFlags.AlwaysAutoResize
        | ImGuiWindowFlags.NoResize
        | ImGuiWindowFlags.NoCollapse
        | ImGuiWindowFlags.NoDecoration
        | ImGuiWindowFlags.NoInputs
        | ImGuiWindowFlags.NoBringToFrontOnFocus
        | ImGuiWindowFlags.NoFocusOnAppearing
        | ImGuiWindowFlags.NoNavFocus
        | ImGuiWindowFlags.NoBackground
        | ImGuiWindowFlags.NoDocking
      )
    {
      this.plugin = plugin;
      this.SizeCondition = ImGuiCond.Always;
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);
    }

    public void DrawBubbles(float gx, float gy, float rx, float ry)
    {
      var size = ImGui.GetIO().DisplaySize;
      var gazeCoord = new Vector2(gx * (size.X / 2) + (size.X / 2), -gy * (size.Y / 2) + (size.Y / 2));
      var rawCoord = new Vector2(rx * (size.X / 2) + (size.X / 2), -ry * (size.Y / 2) + (size.Y / 2));

      var white = ImGui.GetColorU32(new Vector4(1, 1, 1, 1));
      var black = ImGui.GetColorU32(new Vector4(0, 0, 0, 1));
      var red = ImGui.GetColorU32(new Vector4(1, 0, 0, 1));
      var blue = ImGui.GetColorU32(new Vector4(0, 0, 1, 1));
      var green = ImGui.GetColorU32(new Vector4(0, 1, 0, 1));

      const float whiteThick = 3f;
      const float blackThick = 1.5f;

      ImGui.SetNextWindowSize(size);
      ImGui.SetNextWindowViewport(ImGui.GetMainViewport().ID);
      ImGui.SetNextWindowPos(Vector2.Zero);
      const ImGuiWindowFlags flags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar |
                                     ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoDecoration |
                                     ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoBackground |
                                     ImGuiWindowFlags.NoInputs;
      ImGui.Begin("Crosshair Window", flags);

      var dl = ImGui.GetWindowDrawList();

      if (plugin.Configuration.IsVisible)
      {
        foreach (var calibrationPoint in plugin.TrackerService!.CalibrationPoints)
        {
          DrawPoint(calibrationPoint.Reference, dl, red);
          DrawPoint(calibrationPoint.Gaze, dl, blue);
        }

        dl.AddCircle(rawCoord, plugin.Configuration.GazeCircleRadius + blackThick, black, plugin.Configuration.GazeCircleSegments, blackThick);
        dl.AddCircle(rawCoord, plugin.Configuration.GazeCircleRadius - blackThick, black, plugin.Configuration.GazeCircleSegments, blackThick);
        dl.AddCircle(rawCoord, plugin.Configuration.GazeCircleRadius, green, plugin.Configuration.GazeCircleSegments, whiteThick);
      }


      dl.AddCircle(gazeCoord, plugin.Configuration.GazeCircleRadius + blackThick, black, plugin.Configuration.GazeCircleSegments, blackThick);
      dl.AddCircle(gazeCoord, plugin.Configuration.GazeCircleRadius - blackThick, black, plugin.Configuration.GazeCircleSegments, blackThick);
      dl.AddCircle(gazeCoord, plugin.Configuration.GazeCircleRadius, white, plugin.Configuration.GazeCircleSegments, whiteThick);

      ImGui.End();
    }

    public void DrawPoint(Point point, ImDrawListPtr dl, uint color)
    {
      var size = ImGui.GetIO().DisplaySize;
      var xp = point.X * (size.X / 2) + (size.X / 2);
      var yp = -point.Y * (size.Y / 2) + (size.Y / 2);
      var pixelCoord = new Vector2(xp, yp);

      var black = ImGui.GetColorU32(new Vector4(0, 0, 0, 1));

      const float whiteThick = 3f;
      const float blackThick = 1.5f;

      const int radius = 10;
      const int segments = 10;

      dl.AddCircle(pixelCoord, radius + blackThick, black, segments, blackThick);
      dl.AddCircle(pixelCoord, radius - blackThick, black, segments, blackThick);
      dl.AddCircle(pixelCoord, radius, color, segments, whiteThick);
    }

    public override void PreDraw()
    {
      base.PreDraw();

      // Resize overlay to full screen
      Size = ImGui.GetMainViewport().Size;
      Position = ImGui.GetMainViewport().Pos;
    }

    public override void Draw()
    {
      if (plugin.TrackerService == null || (!plugin.Configuration.IsVisible && !plugin.Configuration.OverlayEnabled))
      {
        return;
      }

      if (plugin.Configuration.IsCalibrationEditMode)
      {
        Flags &= ~ImGuiWindowFlags.NoInputs;

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
          var pos = ImGui.GetMousePos();
          var size = ImGui.GetIO().DisplaySize;
          var x = (pos.X - size.X / 2) / (size.X / 2);
          var y = -(pos.Y - size.Y / 2) / (size.Y / 2);
          plugin.TrackerService.AddCalibrationPoint(x, y);
        }
        else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
          plugin.Configuration.IsCalibrationEditMode = false;
        }
      }
      else
      {
        Flags |= ImGuiWindowFlags.NoInputs;
      }

      DrawBubbles(plugin.TrackerService.LastGazeX, plugin.TrackerService.LastGazeY, plugin.TrackerService.LastRawGazeX, plugin.TrackerService.LastRawGazeY);
    }
  }
}
