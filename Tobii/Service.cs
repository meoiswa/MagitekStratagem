using System.Diagnostics.CodeAnalysis;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace TobiiPlugin
{
  public class Service
  {
#pragma warning disable CS8618
    [PluginService] public static Condition Condition { get; private set; }
    [PluginService] public static Framework Framework { get; private set; }
    [PluginService] public static ChatGui ChatGui { get; private set; }
    [PluginService] public static ObjectTable ObjectTable { get; private set; }
    [PluginService] public static ClientState ClientState { get; private set; }
    [PluginService] public static GameGui GameGui { get; private set; }
    [PluginService] public static TargetManager TargetManager { get; private set; }
    [PluginService] public static SigScanner SigScanner { get; private set; }
    [PluginService] public static TargetManager Targetmanager { get; private set; }
#pragma warning restore CS8618
  }
}
