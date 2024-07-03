using Dalamud.Game.Config;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace MagitekStratagemPlugin
{
  public unsafe class MagitekStratagemUI : Window
  {
    private readonly MagitekStratagemPlugin plugin;

    public MagitekStratagemUI(MagitekStratagemPlugin plugin)
  : base(
    "MagitekStratagem##ConfigWindow",
    ImGuiWindowFlags.AlwaysAutoResize
    | ImGuiWindowFlags.NoResize
    | ImGuiWindowFlags.NoCollapse
  )
    {
      this.plugin = plugin;

      SizeConstraints = new WindowSizeConstraints()
      {
        MinimumSize = new Vector2(600, 0),
        MaximumSize = new Vector2(800, 1000)
      };
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

    public override void Draw()
    {
      ImGui.NewLine();
      ImGui.TextWrapped("Disclaimer: This plugin is not officially supported by Tobii. Use at your own risk."
        + " Due to the nature of hot-loading Tobii SDK DLLs, this plugin may crash your game unexpectedly. "
        + " You must have Tobii Game Hub installed, and use a Tobii Eye Tracker that is compatible (4 or 5)");
      ImGui.NewLine();
      ImGui.TextWrapped("In compliance with Tobii guidelines, this plugin will not record nor share Eye Tracking data with any other software component, and Eye Tracking data is immediately disposed after use.");
      ImGui.NewLine();

      ImGui.Separator();

#if !DEBUG
      if (plugin.ErrorHooking)
      {
        ImGui.Text("Error hooking functions.");
        return;
      }
#endif

      if (plugin.TrackerService == null)
      {
        ImGui.Text("Tobii GameHub Not Found");
        if (ImGui.Button("Load Fake Service"))
        {
          plugin.TrackerService = new FakeService(plugin.Configuration.CalibrationPoints);
        }
        return;
      }

      DrawSectionEnabled();

      DrawBehaviorSettingsSection();
      DrawAppareanceSettingsSection();
      DrawGazeCircleSettingsSection();
      DrawRaycastSettingsSection();
      DrawCalibrationSection();

      ImGui.Separator();

      if (plugin.Configuration.Enabled)
      {
        if (!plugin.TrackerService.IsTracking)
        {
          if (ImGui.Button("Start Tracking"))
          {
            plugin.TrackerService.StartTrackingWindow(plugin.PluginInterface.UiBuilder.WindowHandlePtr);
          }
        }
        else
        {
          if (ImGui.Button("Stop Tracking"))
          {
            plugin.TrackerService.StopTracking();
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
      DrawDebugSection();
#endif
    }

    private void DrawDebugSection()
    {
      ImGui.Separator();
      if (ImGui.CollapsingHeader("Debug info"))
      {
        ImGui.Indent();
        ImGui.Text($"Closest Target: {plugin.ClosestMatch?.Name} - {plugin.ClosestMatch?.Address.ToString("X")}");

        var softTarget = Service.TargetManager.SoftTarget;
        if (softTarget != null)
        {
          ImGui.Text($"Soft Target: {softTarget.Name} - {softTarget.Address:X}");
        }

        ImGui.Separator();

        ImGui.Text("Gaze:");
        ImGui.Text($"LastTimestamp: {plugin.TrackerService.LastGazeTimeStamp}");
        ImGui.Text($"LastX: {plugin.TrackerService.LastGazeX}");
        ImGui.Text($"LastY: {plugin.TrackerService.LastGazeY}");

        ImGui.Separator();

        ImGui.Text($"Heatmap: {plugin.GameObjectHeatMap.Count}");
        foreach (var gameObjectHeat in plugin.GameObjectHeatMap)
        {
          ImGui.Text($"{gameObjectHeat.Key} - {gameObjectHeat.Value}");
        }
        ImGui.Unindent();
      }
    }

    private void DrawCalibrationSection()
    {
      ImGui.BeginDisabled(Service.IGameConfig.TryGet(SystemConfigOption.ScreenMode, out uint mode) && mode == 0);
      if (ImGui.CollapsingHeader("Calibration (Not available in Windowed Mode)"))
      {
        ImGui.Indent();
        ImGui.Text("Fine-tuning calibration beyond what your vendor provides.");

        var useCalibration = plugin.Configuration.UseCalibration;
        if (ImGui.Checkbox("Use Calibration", ref useCalibration))
        {
          plugin.Configuration.UseCalibration = useCalibration;
          plugin.Configuration.Save();
        }

        ImGui.NewLine();

        if (!plugin.IsCalibrationEditMode)
        {
          if (ImGui.Button("Enter Calibration Mode"))
          {
            plugin.IsCalibrationEditMode = true;
          }

          if (ImGui.Button("Clear Calibration"))
          {
            plugin.Configuration.CalibrationPoints.Clear();
            plugin.Configuration.Save();
          }
        }
        else
        {
          ImGui.Text("(Right click to exit Calibration Mode)");
        }

        ImGui.NewLine();
        if (ImGui.CollapsingHeader("About Calibration"))
        {
          ImGui.Indent();
          ImGui.TextWrapped("Allows you to fine-tune the calibration of your eye tracker."
           + "\nCalibration points are created on your cursor position,"
           + "\nLook directly at the tip of the cursor, then left-click to create a point."
           + "\nEach calibration point stores the offset between the cursor and your gaze."
           + "\nThe closer your gaze is to a calibration point, the more it affects the result."
           + "\nFor best results, add calibration points close to the edges of the screen."
           + "\nA calibration point near the center might be useful as well.");
          ImGui.Unindent();
        }

        ImGui.Unindent();
      }
      ImGui.EndDisabled();
    }

    private void DrawRaycastSettingsSection()
    {
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
    }

    private void DrawGazeCircleSettingsSection()
    {
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
    }

    private void DrawAppareanceSettingsSection()
    {
      if (ImGui.CollapsingHeader("Appareance Settings"))
      {
        ImGui.Indent();

        DrawOverlayCheckbox();

        ImGui.NewLine();

        DrawProximityColorCombo();

        DrawHighlightColorCombo();

        ImGui.Unindent();
      }
    }

    private void DrawOverlayCheckbox()
    {
      var useOverlay = plugin.Configuration.OverlayEnabled;
      if (ImGui.Checkbox("Always render the Gaze Circle", ref useOverlay))
      {
        plugin.Configuration.OverlayEnabled = useOverlay;
        plugin.Configuration.Save();
      }
      ImGui.TextWrapped("(This option might be disorienting or confusing for some users)");
    }

    private void DrawProximityColorCombo()
    {
      ImGui.Text("Highlight color for proximity target");
      var proximityColor = plugin.Configuration.ProximityColor;
      if (ImGui.Combo("##color", ref proximityColor, "None\0Red\0Green\0Blue\0Yellow\0Orange\0Magenta\0Black\0"))
      {
        proximityColor = ClampColorValue(proximityColor);
        plugin.Configuration.ProximityColor = proximityColor;
        plugin.Configuration.Save();
      }
    }

    private void DrawHighlightColorCombo()
    {
      if (!plugin.Configuration.UseRaycast)
      {
        ImGui.BeginDisabled();
      }

      ImGui.Text("Highlight color for raycasted target");
      var highlightColor = plugin.Configuration.HighlightColor;
      if (ImGui.Combo("##gazecolor", ref highlightColor, "None\0Red\0Green\0Blue\0Yellow\0Orange\0Magenta\0Black\0"))
      {
        highlightColor = ClampColorValue(highlightColor);
        plugin.Configuration.HighlightColor = highlightColor;
        plugin.Configuration.Save();
      }

      if (!plugin.Configuration.UseRaycast)
      {
        ImGui.EndDisabled();
      }
    }

    private int ClampColorValue(int colorValue)
    {
      if (colorValue < 0) return 0;
      if (colorValue > 7) return 7;
      return colorValue;
    }

    private void DrawBehaviorSettingsSection()
    {
      if (ImGui.CollapsingHeader("Behavior Settings"))
      {
        ImGui.Indent();
        ImGui.TextWrapped("These settings control when the Gaze Target overrides happen");

        ImGui.NewLine();
        DrawOverrideEnemyTargetSection();

        ImGui.NewLine();
        DrawOverrideSoftTargetSection();

        ImGui.Unindent();
      }
    }

    private void DrawOverrideEnemyTargetSection()
    {
      var overrideEnemyTarget = plugin.Configuration.OverrideEnemyTarget;
      if (ImGui.Checkbox("Override Enemy Tab Target", ref overrideEnemyTarget))
      {
        plugin.Configuration.OverrideEnemyTarget = overrideEnemyTarget;
        plugin.Configuration.Save();
      }
      ImGui.TextWrapped("Selects your Gaze Target as your Target when you press either one of the 'Cycle through Enemies' keybinds.");

      if (overrideEnemyTarget)
      {
        DrawOverrideEnemyTargetAlwaysSection();
      }
    }

    private void DrawOverrideEnemyTargetAlwaysSection()
    {
      var overrideEnemyTargetAlways = plugin.Configuration.OverrideEnemyTargetAlways;

      if (!overrideEnemyTargetAlways)
      {
        ImGui.TextWrapped("Selects your gaze target only when no target is selected.");
      }
      else
      {
        ImGui.TextWrapped("Selects your gaze target every time you press the keybind.");
      }

      ImGui.Indent();
      if (ImGui.Checkbox("Always override.", ref overrideEnemyTargetAlways))
      {
        plugin.Configuration.OverrideEnemyTargetAlways = overrideEnemyTargetAlways;
        plugin.Configuration.Save();
      }
      ImGui.Unindent();
    }

    private void DrawOverrideSoftTargetSection()
    {
      var overrideSoftTarget = plugin.Configuration.OverrideSoftTarget;
      if (ImGui.Checkbox("Override Soft Tab Target", ref overrideSoftTarget))
      {
        plugin.Configuration.OverrideSoftTarget = overrideSoftTarget;
        plugin.Configuration.Save();
      }
      ImGui.TextWrapped("Selects your Gaze Target as your Target Cursor when you press either one of the 'Target Cursor Left/Right' keybinds.");

      if (overrideSoftTarget)
      {
        DrawOverrideSoftTargetAlwaysSection();
      }
    }

    private void DrawOverrideSoftTargetAlwaysSection()
    {
      var overrideSoftTargetAlways = plugin.Configuration.OverrideSoftTargetAlways;

      if (!overrideSoftTargetAlways)
      {
        ImGui.TextWrapped("Selects your gaze target only when no soft target is present.");
      }
      else
      {
        ImGui.TextWrapped("Selects your gaze target every time you press the keybind.");
      }

      ImGui.Indent();
      if (ImGui.Checkbox("Always override.##soft", ref overrideSoftTargetAlways))
      {
        plugin.Configuration.OverrideSoftTargetAlways = overrideSoftTargetAlways;
        plugin.Configuration.Save();
      }
      ImGui.Unindent();
    }
  }
}
