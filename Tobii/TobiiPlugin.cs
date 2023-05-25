using System;
using System.IO;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Dalamud.Game.ClientState.Objects.Types;

using GameObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using CameraManager = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager;
using System.Numerics;
using ImGuiNET;

namespace TobiiPlugin
{
  public sealed class TobiiPlugin : IDalamudPlugin
  {
    public string Name => "Tobii";

    private const string commandName = "/tobii";

    public DalamudPluginInterface PluginInterface { get; init; }
    public CommandManager CommandManager { get; init; }
    public ChatGui ChatGui { get; init; }
    public Configuration Configuration { get; init; }
    public WindowSystem WindowSystem { get; init; }
    public Condition Condition { get; init; }
    public ObjectTable ObjectTable { get; init; }

    public TobiiUI Window { get; init; }
    public TobiiService TobiiService { get; init; }
    public GameObject? ClosestMatch { get; private set; }

    public TobiiPlugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commandManager,
        [RequiredVersion("1.0")] ChatGui chatGui)
    {
      pluginInterface.Create<Service>();

      PluginInterface = pluginInterface;
      CommandManager = commandManager;
      ChatGui = chatGui;
      WindowSystem = new("TobiiPlugin");

      Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
      Configuration.Initialize(this);

      Condition = Service.Condition;
      ObjectTable = Service.ObjectTable;

      Window = new TobiiUI(this)
      {
        IsOpen = Configuration.IsVisible
      };

      WindowSystem.AddWindow(Window);

      CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
      {
        HelpMessage = "opens the configuration window"
      });

      TobiiService = new TobiiService();

      Service.Framework.Update += OnUpdate;
      PluginInterface.UiBuilder.Draw += DrawUI;
      PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
    }

    public void Dispose()
    {
      TobiiService.Shutdown();

      Service.Framework.Update -= OnUpdate;
      PluginInterface.UiBuilder.Draw -= DrawUI;
      PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;

      WindowSystem.RemoveAllWindows();

      CommandManager.RemoveHandler(commandName);
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
      SetVisible(!Configuration.IsVisible);
    }

    private void DrawUI()
    {
      WindowSystem.Draw();
    }

    private void DrawConfigUI()
    {
      SetVisible(!Configuration.IsVisible);
    }

    public void OnUpdate(Framework framework)
    {
      var player = Service.ClientState.LocalPlayer;
      var position = player?.Position ?? new Vector3();

      if (Configuration.Enabled && Condition.Any() && player != null && TobiiService.IsTracking)
      {
        TobiiService.Update();

        unsafe
        {
          var size = ImGui.GetIO().DisplaySize;
          Vector2 gazeScreenPos = new Vector2(
           TobiiService.LastGazeX * (size.X / 2) + (size.X / 2),
           -TobiiService.LastGazeY * (size.Y / 2) + (size.Y / 2));

          if (Service.GameGui.ScreenToWorld(gazeScreenPos, out Vector3 worldPos))
          {
            var closestDistance = float.MaxValue;

            foreach (var actor in ObjectTable)
            {
              if (actor == null)
              {
                continue;
              }
              var gos = (GameObjectStruct*)actor.Address;
              if (gos->GetIsTargetable() && actor != player)
              {
                var distance = FFXIVClientStructs.FFXIV.Common.Math.Vector3.Distance(worldPos, actor.Position);
                if (ClosestMatch == null)
                {
                  ClosestMatch = actor;
                  closestDistance = distance;
                  continue;
                }
                
                if (closestDistance > distance)
                {
                  ClosestMatch = actor;
                  closestDistance = distance;
                }
              }
            }

            Service.TargetManager.Target = ClosestMatch;
          }
        }
      }
    }
  }
}
