using Dalamud.Logging;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using ImGuiNET;
using System;
using System.Numerics;
using Dalamud.Interface;

namespace TobiiPlugin
{
  // It is good to have this be disposable in general, in case you ever need it
  // to do any cleanup
  public unsafe class TobiiUI : Window, IDisposable
  {
    private readonly TobiiPlugin plugin;

    private bool isFirstDraw = true;

    public TobiiUI(TobiiPlugin plugin)
      : base(
        "Tobii##ConfigWindow",
        ImGuiWindowFlags.AlwaysAutoResize
        | ImGuiWindowFlags.NoResize
        | ImGuiWindowFlags.NoCollapse
      )
    {
      this.plugin = plugin;

      SizeConstraints = new WindowSizeConstraints()
      {
        MinimumSize = new Vector2(468, 0),
        MaximumSize = new Vector2(468, 1000)
      };
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);
    }

    public override void OnClose()
    {
      base.OnClose();
      plugin.Configuration.IsVisible = false;
      plugin.Configuration.Save();
    }

    private void DrawSectionEnabled()
    {
      // can't ref a property, so use a local copy
      var enabled = plugin.Configuration.Enabled;
      if (ImGui.Checkbox("Master Enable", ref enabled))
      {
        plugin.Configuration.Enabled = enabled;
        plugin.Configuration.Save();
      }
    }

    public void DrawCrosshair(float x, float y)
    {
      var size = ImGui.GetIO().DisplaySize;
      var xp = x * (size.X / 2) + (size.X / 2);
      var yp = -y * (size.Y / 2) + (size.Y / 2);
      var pixelCoord = new Vector2(xp, yp);

      var white = ImGui.GetColorU32(new Vector4(1, 1, 1, 1));
      var black = ImGui.GetColorU32(new Vector4(0, 0, 0, 1));

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

      dl.AddCircle(pixelCoord, plugin.Configuration.GazeCircleRadius + blackThick, black, plugin.Configuration.GazeCircleSegments, blackThick);
      dl.AddCircle(pixelCoord, plugin.Configuration.GazeCircleRadius - blackThick, black, plugin.Configuration.GazeCircleSegments, blackThick);
      dl.AddCircle(pixelCoord, plugin.Configuration.GazeCircleRadius, white, plugin.Configuration.GazeCircleSegments, whiteThick);

      ImGui.End();
    }

    public override void Draw()
    {
      if (plugin.ErrorHooking)
      {
        ImGui.Text("Error hooking functions.");
        return;
      }

      DrawSectionEnabled();

      var useRaycast = plugin.Configuration.UseRaycast;
      if (ImGui.Checkbox("Use Raycast", ref useRaycast))
      {
        plugin.Configuration.UseRaycast = useRaycast;
        plugin.Configuration.Save();
      }

      var highlightColor = plugin.Configuration.HighlightColor;
      if (ImGui.Combo("Gaze Color", ref highlightColor, "None\0Red\0Green\0Blue\0Yellow\0Orange\0Magenta\0Black\0"))
      {
        if (highlightColor < 0)
        {
          highlightColor = 0;
        }
        if (highlightColor > 7)
        {
          highlightColor = 7;
        }
        plugin.Configuration.HighlightColor = highlightColor;
        plugin.Configuration.Save();
      }
      
      var proximityColor = plugin.Configuration.ProximityColor;
      if (ImGui.Combo("Proximity Color", ref proximityColor, "None\0Red\0Green\0Blue\0Yellow\0Orange\0Magenta\0Black\0"))
      {
        if (proximityColor < 0)
        {
          proximityColor = 0;
        }
        if (proximityColor > 7)
        {
          proximityColor = 7;
        }
        plugin.Configuration.ProximityColor = proximityColor;
        plugin.Configuration.Save();
      }

      var gazeCircleRadius = plugin.Configuration.GazeCircleRadius;
      if (ImGui.SliderInt("Gaze Circle Radius", ref gazeCircleRadius, 0, 200))
      {
        plugin.Configuration.GazeCircleRadius = gazeCircleRadius;
        plugin.Configuration.Save();
      }

      var gazeCircleSegments = plugin.Configuration.GazeCircleSegments;
      if (ImGui.SliderInt("Gaze Circle Segments", ref gazeCircleSegments, 3, 100))
      {
        plugin.Configuration.GazeCircleSegments = gazeCircleSegments;
        plugin.Configuration.Save();
      }

      var heatIncrement = plugin.Configuration.HeatIncrement;
      if (ImGui.SliderFloat("Heat Increment", ref heatIncrement, 0.01f, 2f))
      {
        plugin.Configuration.HeatIncrement = heatIncrement;
        plugin.Configuration.Save();
      }

      var heatDecay = plugin.Configuration.HeatDecay;
      if (ImGui.SliderFloat("Heat Decay Factor", ref heatDecay, 0.01f, 1f))
      {
        plugin.Configuration.HeatDecay = heatDecay;
        plugin.Configuration.Save();
      }

      ImGui.Text("Tobii Eye Tracker");

      if (plugin.Configuration.Enabled)
      {
        if (!plugin.TobiiService.IsTracking)
        {
          if (ImGui.Button("Start Tracking") || isFirstDraw)
          {
            plugin.TobiiService.StartTrackingWindow(plugin.PluginInterface.UiBuilder.WindowHandlePtr);
            isFirstDraw = false;
          }
        }
        else
        {
          ImGui.Separator();
          ImGui.Text($"Closest Target: {plugin.ClosestMatch?.Name} - {plugin.ClosestMatch?.Address.ToString("X")}");

          ImGui.Separator();

          ImGui.Text("Gaze:");
          ImGui.Text($"LastTimestamp: {plugin.TobiiService.LastGazeTimeStamp}");
          ImGui.Text($"LastX: {plugin.TobiiService.LastGazeX}");
          ImGui.Text($"LastY: {plugin.TobiiService.LastGazeY}");

          ImGui.Separator();

          ImGui.Text($"Heatmap: {plugin.gameObjectHeatMap.Count}");
          foreach (var gameObjectHeat in plugin.gameObjectHeatMap)
          {
            ImGui.Text($"{gameObjectHeat.Key} - {gameObjectHeat.Value}");
          }


          DrawCrosshair(plugin.TobiiService.LastGazeX, plugin.TobiiService.LastGazeY);

          if (ImGui.Button("Stop Tracking"))
          {
            plugin.TobiiService.StopTracking();
          }
        }
      }
    }
  }
}
