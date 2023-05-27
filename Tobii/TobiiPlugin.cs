using System;
using System.IO;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Newtonsoft.Json;
using Dalamud.Game.ClientState.Objects.Types;
using GameObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using System.Numerics;
using ImGuiNET;
using System.Runtime.InteropServices;
using Dalamud.Hooking;

namespace TobiiPlugin
{
  public sealed class TobiiPlugin : IDalamudPlugin
  {
    public string Name => "Tobii";

    private const string commandName = "/tobii";
    private GameObject? lastHighlight = null;

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
    public bool ErrorHooking { get; private set; } = false;

    public delegate void HighlightGameObjectWithColorDelegate(IntPtr gameObject, byte color);
    private delegate IntPtr SelectInitialTabTargetDelegate(IntPtr targetSystem, IntPtr gameObjects, IntPtr camera, IntPtr a4);
    private delegate IntPtr SelectTabTargetDelegate(IntPtr targetSystem, IntPtr camera, IntPtr gameObjects, bool inverse, IntPtr a5);
    private readonly HighlightGameObjectWithColorDelegate? highlightGameObjectWithColor = null;
    private readonly Hook<SelectTabTargetDelegate>? selectTabTargetIgnoreDepthHook = null;
    private readonly Hook<SelectTabTargetDelegate>? selectTabTargetConeHook = null;
    private readonly Hook<SelectInitialTabTargetDelegate>? selectInitialTabTargetHook = null;

    private IntPtr SelectTabTargetIgnoreDepthDetour(IntPtr targetSystem, IntPtr camera, IntPtr gameObjects,
        bool inverse,
        IntPtr a5)
    {
      var originalResult = selectTabTargetIgnoreDepthHook.Original(targetSystem, camera, gameObjects, inverse, a5);
      if (Configuration.Enabled && ClosestMatch != null)
      {
        PluginLog.Log($"SelectTabTargetIgnoreDepthDetour - Override tab target {originalResult:X} with {ClosestMatch.Address:X}");
        return ClosestMatch.Address;
      }
      return originalResult;
    }

    private IntPtr SelectTabTargetConeDetour(IntPtr targetSystem, IntPtr camera, IntPtr gameObjects, bool inverse,
        IntPtr a5)
    {
      var originalResult = selectTabTargetConeHook.Original(targetSystem, camera, gameObjects, inverse, a5);
      if (Configuration.Enabled && ClosestMatch != null)
      {
        PluginLog.Log($"SelectTabTargetConeDetour - Override tab target {originalResult:X} with {ClosestMatch.Address:X}");
        return ClosestMatch.Address;
      }
      return originalResult;
    }

    private IntPtr SelectInitialTabTargetDetour(IntPtr targetSystem, IntPtr gameObjects, IntPtr camera, IntPtr a4)
    {
      var originalResult = selectInitialTabTargetHook.Original(targetSystem, gameObjects, camera, a4);
      PluginLog.Log($"SelectInitialTabTargetDetour - {originalResult:X}");
      return originalResult;
    }

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

      // Sig courtesy of Wintermute
      if (Service.SigScanner.TryScanText("E8 ?? ?? ?? FF 48 8D 8B ?? ?? 00 00 40 0F B6 D6 E8 ?? ?? ?? ?? 40 84 FF", out var highlightGameObjectSigAddr))
      {
        PluginLog.LogDebug("Found SIG for HighlightGameObjectWithColor", highlightGameObjectSigAddr.ToString("X"));
        highlightGameObjectWithColor = Marshal.GetDelegateForFunctionPointer<HighlightGameObjectWithColorDelegate>(highlightGameObjectSigAddr);
      }
      else
      {
        PluginLog.LogDebug("Failed to adquire SIG for HighlightGameObjectWithColor");
        ErrorHooking = true;
      }

      // Sig courtesy of Avaflow
      if (Service.SigScanner.TryScanText("E8 ?? ?? ?? ?? 48 8B C8 48 85 C0 74 27", out var selectTabTargetIgnoreDepthAddr))
      {
        PluginLog.LogDebug("Found SIG for SelectTabTargetIgnoreDepth", selectTabTargetIgnoreDepthAddr.ToString("X"));
        selectTabTargetIgnoreDepthHook = Hook<SelectTabTargetDelegate>.FromAddress(selectTabTargetIgnoreDepthAddr, new SelectTabTargetDelegate(SelectTabTargetIgnoreDepthDetour));
        selectTabTargetIgnoreDepthHook.Enable();
      }
      else
      {
        PluginLog.LogDebug("Failed to adquire SIG for SelectTabTargetIgnoreDepth");
        ErrorHooking = true;
      }

      // Sig courtesy of Avaflow
      if (Service.SigScanner.TryScanText("E8 ?? ?? ?? ?? EB 4C 41 B1 01", out var selectTabTargetConeAddr))
      {
        PluginLog.LogDebug("Found SIG for SelectTabTargetCone", selectTabTargetConeAddr.ToString("X"));
        selectTabTargetConeHook = Hook<SelectTabTargetDelegate>.FromAddress(selectTabTargetConeAddr, new SelectTabTargetDelegate(SelectTabTargetConeDetour));
        selectTabTargetConeHook.Enable();
      }
      else
      {
        PluginLog.LogDebug("Failed to adquire SIG for SelectTabTargetCone");
        ErrorHooking = true;
      }

      // Sig courtesy of Avaflow
      if (Service.SigScanner.TryScanText("E8 ?? ?? ?? ?? EB 37 48 85 C9", out var selectInitialTabTargetSigAddr))
      {
        PluginLog.LogDebug("Found SIG for SelectInitialTabTarget", selectInitialTabTargetSigAddr.ToString("X"));
        selectInitialTabTargetHook = Hook<SelectInitialTabTargetDelegate>.FromAddress(selectInitialTabTargetSigAddr, new SelectInitialTabTargetDelegate(SelectInitialTabTargetDetour));
        selectInitialTabTargetHook.Enable();
      }
      else
      {
        PluginLog.LogDebug("Failed to adquire SIG for SelectInitialTabTarget");
        ErrorHooking = true;
      }

      Service.Framework.Update += OnUpdate;
      PluginInterface.UiBuilder.Draw += DrawUI;
      PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
    }

    public void Dispose()
    {
      selectInitialTabTargetHook?.Dispose();
      selectTabTargetConeHook?.Dispose();
      selectTabTargetIgnoreDepthHook?.Dispose();

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
      if (selectTabTargetConeHook == null || selectTabTargetIgnoreDepthHook == null || selectInitialTabTargetHook == null || highlightGameObjectWithColor == null)
      {
        ErrorHooking = true;
        return;
      }

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

            if (ClosestMatch != null)
            {
              if (lastHighlight != null && lastHighlight.Address != ClosestMatch.Address)
              {
                highlightGameObjectWithColor(lastHighlight.Address, 0);
                lastHighlight = null;
              }

              highlightGameObjectWithColor(ClosestMatch.Address, (byte)Configuration.HighlightColor);
              lastHighlight = ClosestMatch;


            }
            if (ClosestMatch == null)
            {
              if (lastHighlight != null)
              {
                highlightGameObjectWithColor(lastHighlight.Address, 0);
                lastHighlight = null;
              }
            }
          }
        }
      }
      else
      {
        unsafe
        {
          if (lastHighlight != null)
          {
            highlightGameObjectWithColor(lastHighlight.Address, 0);
            lastHighlight = null;
          }
        }
      }
    }
  }
}
