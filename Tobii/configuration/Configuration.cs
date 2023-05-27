using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace TobiiPlugin
{
  [Serializable]
  public class Configuration : IPluginConfiguration
  {
    public virtual int Version { get; set; } = 0;

    public bool Enabled { get; set; } = true;

    public bool IsVisible { get; set; } = false;

    public bool AutoStartTracking { get; set; } = true;

    public int HighlightColor { get; set; } = 2;
    public int ProximityColor { get; set; } = 5;

    public bool OverlayEnabled { get; set; } = false;

    public bool UseRaycast { get; set; } = true;

    public int GazeCircleSegments { get; set; } = 24;
    public int GazeCircleRadius { get; set; } = 100;

    public float HeatIncrement { get; set; } = 1.0f;
    public float HeatDecay { get; set; } = 0.5f;
    
    public bool InitialTabTargetEnabled { get; set; } = true;
    public bool TabTargetEnabled { get; set; } = false;

    // the below exist just to make saving less cumbersome
    [NonSerialized]
    private TobiiPlugin? plugin;
    public void Initialize(TobiiPlugin plugin) => this.plugin = plugin;
    public void Save()
    {
      plugin!.SaveConfiguration();
    }
  }
}
