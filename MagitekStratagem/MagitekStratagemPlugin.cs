using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Newtonsoft.Json;

namespace MagitekStratagemPlugin
{
  public sealed unsafe class MagitekStratagemPlugin : IDalamudPlugin
  {
    public string Name => "Magitek Stratagem";

    private const string commandName = "/magiteks";
    private const string overlayCommandName = "/magiteksoverlay";

    public IDalamudPluginInterface PluginInterface { get; init; }
    public ICommandManager CommandManager { get; init; }
    public Configuration Configuration { get; init; }
    public WindowSystem WindowSystem { get; init; }
    public MagitekStratagemUI Window { get; init; }
    public MagitekStratagemOverlay Overlay { get; init; }
    public SignalRService SignalRService { get; init; }
    public SelectTargetHooksService SelectTargetHooksService { get; init; }
    public GameObjectHeatmapService HeatmapService { get; init; }
    public GazeService GazeService { get; init; }
    public SharedDataService SharedDataService { get; init; }

    public MagitekStratagemPlugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager)
    {
      pluginInterface.Create<Service>();
      PluginInterface = pluginInterface;

      CommandManager = commandManager;
      WindowSystem = new("MagitekStratagemPlugin");

      Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
      Configuration.Initialize(this);

      SignalRService = new SignalRService(PluginInterface.AssemblyLocation, Configuration);
      HeatmapService = new GameObjectHeatmapService(Configuration);
      GazeService = new GazeService(HeatmapService, Configuration);
      SelectTargetHooksService = new SelectTargetHooksService(GazeService, Configuration);
      SharedDataService = new SharedDataService(PluginInterface);

      Window = new MagitekStratagemUI(this)
      {
        IsOpen = Configuration.IsVisible
      };

      Overlay = new MagitekStratagemOverlay(this)
      {
        IsOpen = true
      };

      WindowSystem.AddWindow(Window);
      WindowSystem.AddWindow(Overlay);

      CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
      {
        HelpMessage = "opens the configuration window"
      });
      CommandManager.AddHandler(overlayCommandName, new CommandInfo(OnCommand)
      {
        HelpMessage = "togggles the overlay"
      });

      SelectTargetHooksService.EnableHooks();

      Service.Framework.Update += OnUpdate;
      PluginInterface.UiBuilder.Draw += DrawUI;
      PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
      PluginInterface.UiBuilder.OpenMainUi += DrawConfigUI;
    }

    public void Dispose()
    {
      SignalRService.Dispose();
      GazeService?.Dispose();
      SelectTargetHooksService?.Dispose();
      SharedDataService?.Dispose();
      HeatmapService?.Dispose();

      Service.Framework.Update -= OnUpdate;
      PluginInterface.UiBuilder.Draw -= DrawUI;
      PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
      PluginInterface.UiBuilder.OpenMainUi -= DrawConfigUI;

      WindowSystem.RemoveAllWindows();

      CommandManager.RemoveHandler(commandName);
      CommandManager.RemoveHandler(overlayCommandName);
    }

    public void SaveConfiguration()
    {
      var configJson = JsonConvert.SerializeObject(Configuration, Formatting.Indented);
      File.WriteAllText(PluginInterface.ConfigFile.FullName, configJson);
    }

    private void SetVisible(bool isVisible)
    {
      Configuration.IsVisible = isVisible;
      Configuration.Save();

      Window.IsOpen = Configuration.IsVisible;
    }

    private void OnCommand(string command, string args)
    {
      if (command == commandName)
      {
        SetVisible(!Configuration.IsVisible);
      }
      else if (command == overlayCommandName)
      {
        Configuration.OverlayEnabled = !Configuration.OverlayEnabled;
        Configuration.Save();
      }
    }

    private void DrawUI()
    {
      WindowSystem.Draw();
    }

    private void DrawConfigUI()
    {
      SetVisible(!Configuration.IsVisible);
    }

    public void OnUpdate(IFramework framework)
    {
      SignalRService.Update();
      GazeService.Update(Service.ObjectTable.LocalPlayer, SignalRService.ActiveTracker);
      if (SignalRService.ActiveTracker != null)
      {
        SharedDataService.Update(SignalRService.ActiveTracker);
      }
    }
  }
}
