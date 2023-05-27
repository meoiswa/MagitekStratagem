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

      if (ImGui.CollapsingHeader("Behavior Settings"))
      {
        ImGui.Indent();

        ImGui.TextWrapped("These settings control when the Gaze Target overrides happen");

        ImGui.NewLine();
        var initialTabTargetEnabled = plugin.Configuration.InitialTabTargetEnabled;
        if (ImGui.Checkbox("Override Initial Enemy Tab Target", ref initialTabTargetEnabled))
        {
          plugin.Configuration.InitialTabTargetEnabled = initialTabTargetEnabled;
          plugin.Configuration.Save();
        }
        ImGui.TextWrapped("Selects your gaze target only when no target is selected.");
        ImGui.TextWrapped("Only works with either 'Cycle through Enemies' keybind.");

        ImGui.NewLine();
        var tabTargetEnabled = plugin.Configuration.TabTargetEnabled;
        if (ImGui.Checkbox("Override ALL tab targets", ref tabTargetEnabled))
        {
          plugin.Configuration.TabTargetEnabled = tabTargetEnabled;
          plugin.Configuration.Save();
        }
        ImGui.TextWrapped("Selects your gaze target every time you tab target");
        ImGui.TextWrapped("Fully overrides both 'Cycle through Enemies' and 'Target Cursor Left/Right' keybinds.");

        ImGui.Unindent();
      }


      if (ImGui.CollapsingHeader("Appareance Settings"))
      {
        ImGui.Indent();
        var useOverlay = plugin.Configuration.OverlayEnabled;
        if (ImGui.Checkbox("Always render the Gaze Circle", ref useOverlay))
        {
          plugin.Configuration.OverlayEnabled = useOverlay;
          plugin.Configuration.Save();
        }
        ImGui.TextWrapped("(This option might be disorienting or confusing for some users)");

        ImGui.NewLine();
        ImGui.Text("Highlight color for proximity target");
        var proximityColor = plugin.Configuration.ProximityColor;
        if (ImGui.Combo("##color", ref proximityColor, "None\0Red\0Green\0Blue\0Yellow\0Orange\0Magenta\0Black\0"))
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

        if (!plugin.Configuration.UseRaycast)
        {
          ImGui.BeginDisabled();
        }
        ImGui.Text("Highlight color for raycasted target");
        var highlightColor = plugin.Configuration.HighlightColor;
        if (ImGui.Combo("##gazecolor", ref highlightColor, "None\0Red\0Green\0Blue\0Yellow\0Orange\0Magenta\0Black\0"))
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
        if (!plugin.Configuration.UseRaycast)
        {
          ImGui.EndDisabled();
        }
        ImGui.Unindent();
      }

      if (ImGui.CollapsingHeader("Gaze Circle Settings"))
      {
        ImGui.Indent();
        ImGui.Text("Repesents the location of your gaze.");
        ImGui.TextWrapped("Eye-tracking is not perfect, so we draw a circle around your gaze to account for the inaccuracy.");

        ImGui.NewLine();
        ImGui.Text("Gaze Circle Radius (default = 100)");
        var gazeCircleRadius = plugin.Configuration.GazeCircleRadius;
        if (ImGui.SliderInt("##radius", ref gazeCircleRadius, 0, 200))
        {
          plugin.Configuration.GazeCircleRadius = gazeCircleRadius;
          plugin.Configuration.Save();
        }
        ImGui.TextWrapped("Smaller radius are more precise, but may drift off your actual sight position."
         + " Larger radius makes it harder to target specific things, but is more forgiving."
         + " It might be worth experimenting with a smaller radius even if"
         + " it drifts away from your sight at the edges of the screen, as most of the"
         + " action happens close to the center of the screen anyways");

        ImGui.NewLine();
        ImGui.Text("Gaze Circle Segments (default = 24)");
        var gazeCircleSegments = plugin.Configuration.GazeCircleSegments;
        if (ImGui.SliderInt("##segments", ref gazeCircleSegments, 3, 100))
        {
          plugin.Configuration.GazeCircleSegments = gazeCircleSegments;
          plugin.Configuration.Save();
        }
        ImGui.TextWrapped("This setting also affects the number of rays casted, when enabled.");
        ImGui.TextWrapped("Entirely cosmetic otherwise.");

        ImGui.Unindent();
      }

      if (ImGui.CollapsingHeader("Raycast Settings"))
      {
        ImGui.Indent();
        ImGui.Text("Raycasting is slower, but more accurate.");

        var useRaycast = plugin.Configuration.UseRaycast;
        if (ImGui.Checkbox("Use Raycast", ref useRaycast))
        {
          plugin.Configuration.UseRaycast = useRaycast;
          plugin.Configuration.Save();
        }

        ImGui.NewLine();
        if (ImGui.CollapsingHeader("About Raycast"))
        {
          ImGui.Indent();
          ImGui.TextWrapped("Behaves similarly to clicking on the target."
           + "\nRays are casted within the Gaze Circle Radius."
           + "\nOne ray per Gaze Circle Segment is casted every frame."
           + "\nWorks better with smaller Radius and more Segments."
           + "\nIs not subject to Parallax and other projection issues.");
          ImGui.Unindent();
        }

        if (ImGui.CollapsingHeader("Advanced Raycast Settings"))
        {
          ImGui.Indent();
          ImGui.TextWrapped("Objects hit by rays accumulate 'heat'."
           + "\nThe object with the most heat is selected as the gaze target."
           + "\nHeat is lost over time, and gained when hit by a ray."
           + "\nHeat tracking prevents jitter and flickering.");

          ImGui.NewLine();
          ImGui.Text("Heat gained when an object it is hit by a raycast (default = 1.0)");
          var heatIncrement = plugin.Configuration.HeatIncrement;
          if (ImGui.SliderFloat("##increment", ref heatIncrement, 0.01f, 2f))
          {
            plugin.Configuration.HeatIncrement = heatIncrement;
            plugin.Configuration.Save();
          }

          ImGui.NewLine();
          ImGui.Text("Factor by which heat decays each frame (default = 0.5)");
          var heatDecay = plugin.Configuration.HeatDecay;
          if (ImGui.SliderFloat("##decay", ref heatDecay, 0.01f, 1f))
          {
            plugin.Configuration.HeatDecay = heatDecay;
            plugin.Configuration.Save();
          }

          ImGui.Unindent();
        }
        ImGui.Unindent();
      }

      ImGui.Separator();

      if (plugin.Configuration.Enabled)
      {
        if (!plugin.TobiiService.IsTracking)
        {
          if (ImGui.Button("Start Tracking"))
          {
            plugin.TobiiService.StartTrackingWindow(plugin.PluginInterface.UiBuilder.WindowHandlePtr);
          }
        }
        else
        {
          if (ImGui.Button("Stop Tracking"))
          {
            plugin.TobiiService.StopTracking();
          }
        }
      }

      ImGui.SameLine();

      var autoStart = plugin.Configuration.AutoStartTracking;
      if (ImGui.Checkbox("Auto-Start", ref autoStart))
      {
        plugin.Configuration.AutoStartTracking = autoStart;
        plugin.Configuration.Save();
      }
      ImGui.SameLine();
      ImGui.Text("(Start tracking on game start)");

#if DEBUG
      ImGui.Separator();
      if (ImGui.CollapsingHeader("Debug info"))
      {
        ImGui.Indent();
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
        ImGui.Unindent();
      }
#endif
    }
  }
}
