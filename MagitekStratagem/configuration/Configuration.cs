using Dalamud.Configuration;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace MagitekStratagemPlugin
{
  [Serializable]
  public class Configuration : IPluginConfiguration
  {
    public virtual int Version { get; set; } = 1;

    public bool Enabled { get; set; } = true;

    public bool IsVisible { get; set; } = false;

    public ObjectHighlightColor HighlightColor { get; set; } = ObjectHighlightColor.Green;
    public ObjectHighlightColor ProximityColor { get; set; } = ObjectHighlightColor.Orange;

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

    public byte AddonOutOfGazeAlpha { get; set; } = 0x00;

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
