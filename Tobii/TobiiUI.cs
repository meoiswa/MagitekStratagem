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

      dl.AddCircle(new Vector2(xp, yp), 100 + blackThick, black, 24, blackThick);
      dl.AddCircle(new Vector2(xp, yp), 100 - blackThick, black, 24, blackThick);
      dl.AddCircle(new Vector2(xp, yp), 100, white, 24, whiteThick);

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

      var highlightColor = plugin.Configuration.HighlightColor;
      if (ImGui.Combo("Highlight Mode", ref highlightColor, "None\0Red\0Greem\0Blue\0Yellow\0Orange\0Magenta\0Black\0"))
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
