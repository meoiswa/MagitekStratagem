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
        | ImGuiWindowFlags.NoMove
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

      ForceMainWindow = true;
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);
    }

    public void DrawBubbles(float gx, float gy, float rx, float ry)
    {
      var size = ImGui.GetMainViewport().Size;
      var gazeCoord = new Vector2(gx * (size.X / 2) + (size.X / 2), -gy * (size.Y / 2) + (size.Y / 2)) + ImGui.GetMainViewport().Pos;
      var rawCoord = new Vector2(rx * (size.X / 2) + (size.X / 2), -ry * (size.Y / 2) + (size.Y / 2)) + ImGui.GetMainViewport().Pos;

      var white = ImGui.GetColorU32(new Vector4(1, 1, 1, 1));
      var black = ImGui.GetColorU32(new Vector4(0, 0, 0, 1));
      var red = ImGui.GetColorU32(new Vector4(1, 0, 0, 1));
      var blue = ImGui.GetColorU32(new Vector4(0, 0, 1, 1));
      var yellow = ImGui.GetColorU32(new Vector4(1, 1, 0, 1));

      const float whiteThick = 3f;
      const float blackThick = 1.5f;

      var dl = ImGui.GetWindowDrawList();

      dl.AddCircle(gazeCoord, plugin.Configuration.GazeCircleRadius + blackThick, black, plugin.Configuration.GazeCircleSegments, blackThick);
      dl.AddCircle(gazeCoord, plugin.Configuration.GazeCircleRadius - blackThick, black, plugin.Configuration.GazeCircleSegments, blackThick);
      dl.AddCircle(gazeCoord, plugin.Configuration.GazeCircleRadius, white, plugin.Configuration.GazeCircleSegments, whiteThick);
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
      Position = Vector2.Zero;
    }

    public override void Draw()
    {
      if (plugin.TrackerService == null || (!plugin.Configuration.IsVisible && !plugin.Configuration.OverlayEnabled))
      {
        return;
      }

      DrawBubbles(plugin.TrackerService.LastGazeX, plugin.TrackerService.LastGazeY, plugin.TrackerService.LastGazeX, plugin.TrackerService.LastGazeY);
    }
  }
}
