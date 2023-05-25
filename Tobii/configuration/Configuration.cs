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
