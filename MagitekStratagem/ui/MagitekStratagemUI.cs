using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Dalamud.Bindings.ImGui;
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

    private void DrawServiceSelectorSection()
    {
      if (ImGui.CollapsingHeader("Service Selector"))
      {
        ImGui.Indent();
        ImGui.TextWrapped("Select the service to use for eye tracking.");

        if (ImGui.Button("Refresh"))
        {
          plugin.SignalRService.RefreshServices();
        }

        var trackers = plugin.SignalRService.GetTrackers();
        var array = trackers.OrderBy(x => x.FullName).ToArray();
        var current = Array.FindIndex(array, x => x.FullName == plugin.Configuration.SelectedTrackerFullName);
        var namesString = string.Join("\0", array.Select(x => x.Name));

        if (ImGui.Combo("##service", ref current, namesString))
        {
          plugin.Configuration.SelectedTrackerFullName = array[current].FullName;
          plugin.Configuration.SelectedTrackerName = array[current].Name;
          plugin.Configuration.Save();
        }

        ImGui.Unindent();
      }
    }

    private void DrawDisclosureSection()
    {
      if (ImGui.CollapsingHeader("Tracker Service Information"))
      {
        ImGui.TextWrapped("IMPORTANT: due to changes in newer versions, only fullscreen or borderless windowed mode"
          + " is supported temporarily. Check the \"Plugins by Meoiswa\" #plugin-help-forum"
          + " post on the official Dalamud Discord for more information.");
        ImGui.NewLine();

        if (plugin.SignalRService.ActiveTracker == null)
        {
          ImGui.TextWrapped("No service selected.");
        }
        else if (plugin.SignalRService.ActiveTracker.Name == "Fake Eye")
        {
          ImGui.TextWrapped("Fake Service is selected. Uses your mouse cursor as a fake gaze tracker.");
        }
        else if (plugin.SignalRService.ActiveTracker.Name == "Tobii")
        {
          ImGui.TextWrapped("Tobii Eye Tracker 5");
          ImGui.TextWrapped("Next generation head and eye tracking");
          ImGui.NewLine();
          ImGui.TextWrapped("Disclaimer: This plugin is not officially supported by Tobii. Use at your own risk."
          + " You must have Tobii Game Hub installed, and use a Tobii Eye Tracker that is compatible (4 or 5).");
          ImGui.TextWrapped("In compliance with Tobii guidelines, this plugin will not record nor share Eye Tracking"
          + "data with any other software component, and Eye Tracking data is immediately disposed after use.");
        }
        else if (plugin.SignalRService.ActiveTracker.Name == "Eyeware Beam")
        {
          ImGui.TextWrapped("Eyeware Beam Eye Tracker: Turn Your Webcam into an Eye Tracker");
          ImGui.TextWrapped("Say goodbye to bulky hardware trackers for gaming. Upgrade your webcam with AI-powered eye tracking software now!");

          if (ImGui.Button("Get Eyeware Beam Eye Tracker (Affiliate link)"))
          {
            Dalamud.Utility.Util.OpenLink("https://beam.eyeware.tech/?via=meoiswa");
          }
          ImGui.NewLine();
          ImGui.TextWrapped("Disclaimer: This plugin is not officially supported by Eyewear. Use at your own risk."
          + " You must have Eyewear Beam Eye Tracker installed, and have a valid active license.");
          ImGui.TextWrapped("In compliance with Eyewear Beam guidelines, this plugin will not record nor share Eye Tracking"
          + " data with any other software component, and Eye Tracking data is immediately disposed after use.");
        }
      }
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

      if (plugin.SignalRService.State != Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
      {
        ImGui.Text("SignalR Service is not connected.");

        ImGui.TextWrapped("MagitekStratagemServer is required for this plugin to work."
          + "\nCurrently only available as a separate download on GitHub."
          + "\nhttps://github.com/meoiswa/MagitekStratagemServer/releases/tag/v0.0.1");
        if (ImGui.Button("Reconnect"))
        {
          _ = plugin.SignalRService.Start();
        }
        return;
      }

      DrawServiceSelectorSection();

      DrawDisclosureSection();

      ImGui.Separator();

#if !DEBUG
      if (plugin.SelectTargetHooksService.ErrorHooking)
      {
        ImGui.Text("Error hooking functions.");
        return;
      }
#endif

      if (plugin.SignalRService.ActiveTracker == null)
      {
        ImGui.Text("No Tracker Selected");
        return;
      }

      DrawSectionEnabled();

      DrawBehaviorSettingsSection();
      DrawAppareanceSettingsSection();
      DrawTrackerSettingsSection();
      DrawGazeCircleSettingsSection();
      DrawRaycastSettingsSection();

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
        ImGui.Text($"Closest Target: {plugin.GazeService.ClosestMatch?.Name} - {plugin.GazeService.ClosestMatch?.Address.ToString("X")}");
        ImGui.Text($"Is Raycasted: {plugin.GazeService.IsRaycasted}");

        ImGui.Separator();
        ImGui.Text("Hooks:");
        ImGui.Indent();
        ImGui.Text($"SelectInitialTabTarget: {plugin.SelectTargetHooksService.SelectInitialTabTargetHooked}");
        ImGui.Text($"SelectTabTargetCone: {plugin.SelectTargetHooksService.SelectTabTargetConeHooked}");
        ImGui.Text($"SelectTabTargetIgnoreDepth: {plugin.SelectTargetHooksService.SelectTabTargetIgnoreDepthHooked}");
        ImGui.Text($"GetInputStatus: {plugin.SelectTargetHooksService.GetInputStatusHooked}");
        ImGui.Unindent();

        var softTarget = Service.TargetManager.SoftTarget;
        if (softTarget != null)
        {
          ImGui.Text($"Soft Target: {softTarget.Name} - {softTarget.Address:X}");
        }

        ImGui.Separator();

        if (plugin.SignalRService.ActiveTracker != null)
        {
          ImGui.Text("Tracking: " + plugin.SignalRService.ActiveTracker.IsTracking);
          ImGui.Text("Gaze:");
          ImGui.Indent();
          ImGui.Text($"LastTime: {plugin.SignalRService.ActiveTracker.LastGazeTimestamp}");
          ImGui.Text($"LastX: {plugin.SignalRService.ActiveTracker.LastGazePos.X}");
          ImGui.Text($"LastY: {plugin.SignalRService.ActiveTracker.LastGazePos.Y}");
          ImGui.Unindent();
          ImGui.Text("Head:");
          ImGui.Indent();
          ImGui.Text($"LastTime: {plugin.SignalRService.ActiveTracker.LastHeadTimestamp}");
          ImGui.Text($"LastHeadX: {plugin.SignalRService.ActiveTracker.LastHeadPosition.X}");
          ImGui.Text($"LastHeadY: {plugin.SignalRService.ActiveTracker.LastHeadPosition.Y}");
          ImGui.Text($"LastHeadZ: {plugin.SignalRService.ActiveTracker.LastHeadPosition.Z}");
          ImGui.Text($"LastHeadPitch: {plugin.SignalRService.ActiveTracker.LastHeadRotation.X}");
          ImGui.Text($"LastHeadYaw: {plugin.SignalRService.ActiveTracker.LastHeadRotation.Y}");
          ImGui.Text($"LastHeadRoll: {plugin.SignalRService.ActiveTracker.LastHeadRotation.Z}");
          ImGui.Unindent();
          ImGui.Text("GazeService:");
          ImGui.Indent();
          ImGui.Text($"LastGazeScreenPos: {plugin.GazeService.LastGazeScreenPos.X}, {plugin.GazeService.LastGazeScreenPos.Y}");
          ImGui.Text($"LastGazeWorldPos: {plugin.GazeService.LastGazeWorldPos.X}, {plugin.GazeService.LastGazeWorldPos.Y}, {plugin.GazeService.LastGazeWorldPos.Z}");
        }
        else
        {
          ImGui.Text("No Active Tracker");

          ImGui.Separator();

          ImGui.Text($"Heatmap: {plugin.HeatmapService.HeatMap.Count}");
          foreach (var gameObjectHeat in plugin.HeatmapService.HeatMap)
          {
            ImGui.Text($"{gameObjectHeat.Key} - {gameObjectHeat.Value}");
          }
          ImGui.Unindent();
        }
      }
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

    private void DrawTrackerSettingsSection()
    {
      if (ImGui.CollapsingHeader("Tracker Settings"))
      {
        ImGui.Indent();

        ImGui.TextWrapped("Settings for using tracker head rotation as the gaze position.");

        var useHead = plugin.Configuration.UseHeadRotationAsGaze;
        if (ImGui.Checkbox("Use Head Rotation as Gaze", ref useHead))
        {
          plugin.Configuration.UseHeadRotationAsGaze = useHead;
          plugin.Configuration.Save();
        }

        ImGui.NewLine();
        ImGui.Text("Max Pitch Angle (degrees)");
        var maxPitch = plugin.Configuration.MaxPitchAngle;
        if (ImGui.SliderInt("##maxpitch", ref maxPitch, 1, 90))
        {
          plugin.Configuration.MaxPitchAngle = maxPitch;
          plugin.Configuration.Save();
        }

        ImGui.Text("Max Yaw Angle (degrees)");
        var maxYaw = plugin.Configuration.MaxYawAngle;
        if (ImGui.SliderInt("##maxyaw", ref maxYaw, 1, 90))
        {
          plugin.Configuration.MaxYawAngle = maxYaw;
          plugin.Configuration.Save();
        }

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
      var proximityColor = (int)plugin.Configuration.ProximityColor;
      if (ImGui.Combo("##color", ref proximityColor, "None\0Red\0Green\0Blue\0Yellow\0Orange\0Magenta\0"))
      {
        proximityColor = ClampColorValue(proximityColor);
        plugin.Configuration.ProximityColor = (ObjectHighlightColor)proximityColor;
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
      var highlightColor = (int)plugin.Configuration.HighlightColor;
      if (ImGui.Combo("##gazecolor", ref highlightColor, "None\0Red\0Green\0Blue\0Yellow\0Orange\0Magenta\0"))
      {
        highlightColor = ClampColorValue(highlightColor);
        plugin.Configuration.HighlightColor = (ObjectHighlightColor)highlightColor;
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
