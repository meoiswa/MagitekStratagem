using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace MagitekStratagemPlugin
{
  [Serializable]
  public class Configuration : IPluginConfiguration
  {
    public virtual int Version { get; set; } = 1;

    public bool Enabled { get; set; } = true;

    public bool IsVisible { get; set; } = false;

    public int HighlightColor { get; set; } = 2;
    public int ProximityColor { get; set; } = 5;

    public bool OverlayEnabled { get; set; } = false;

    public bool UseRaycast { get; set; } = true;

    public int GazeCircleSegments { get; set; } = 24;
    public int GazeCircleRadius { get; set; } = 100;

    public float HeatIncrement { get; set; } = 1.0f;
    public float HeatDecay { get; set; } = 0.5f;

    public bool OverrideEnemyTarget { get; set; } = true;
    public bool OverrideEnemyTargetAlways { get; set; } = false;
    public bool OverrideSoftTarget { get; set; } = true;
    public bool OverrideSoftTargetAlways { get; set; } = false;
    public string SelectedTrackerFullName { get; set; } = string.Empty;
    public string SelectedTrackerName { get; set; } = string.Empty;

    // the below exist just to make saving less cumbersome
    [NonSerialized]
    private MagitekStratagemPlugin? plugin;
    public void Initialize(MagitekStratagemPlugin plugin) => this.plugin = plugin;
    public void Save()
    {
      plugin!.SaveConfiguration();
    }
  }
}
