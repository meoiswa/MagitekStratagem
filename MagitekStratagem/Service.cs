using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace MagitekStratagemPlugin
{
  public class Service
  {
#pragma warning disable CS8618
    [PluginService] public static IClientState ClientState { get; private set; }
    [PluginService] public static ICondition Condition { get; private set; }
    [PluginService] public static IFramework Framework { get; private set; }
    [PluginService] public static IGameGui GameGui { get; private set; }
    [PluginService] public static IObjectTable ObjectTable { get; private set; }
    [PluginService] public static IPluginLog PluginLog { get; private set; }
    [PluginService] public static ITargetManager TargetManager { get; private set; }
    [PluginService] public static IGameInteropProvider IGameInterop { get; private set; }
#pragma warning restore CS8618
  }
}
