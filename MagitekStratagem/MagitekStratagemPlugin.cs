using System;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Newtonsoft.Json;
using Dalamud.Game.ClientState.Objects.Types;
using GameObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using System.Numerics;
using ImGuiNET;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System.Linq;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using System.Text.RegularExpressions;
using Dalamud.Utility.Signatures;
using Dalamud.Game.Config;
using FFXIVClientStructs.FFXIV.Client.UI;
using MagitekStratagemPlugin.Tobii;
using MagitekStratagemPlugin.Eyeware;
using Dalamud.Game.Gui.PartyFinder.Types;
using Dalamud.Game.ClientState.Conditions;

namespace MagitekStratagemPlugin
{
  public sealed unsafe class MagitekStratagemPlugin : IDalamudPlugin
  {
    public string Name => "Magitek Stratagem";

    private const string commandName = "/magiteks";
    private const string overlayCommandName = "/magiteksoverlay";

    private unsafe uint? lastHighlightEntityId = null;

    private bool autoStarted;

    public IDalamudPluginInterface PluginInterface { get; init; }
    public ICommandManager CommandManager { get; init; }
    public Configuration Configuration { get; init; }
    public WindowSystem WindowSystem { get; init; }
    public Random Random { get; }
    public MagitekStratagemUI Window { get; init; }
    public MagitekStratagemOverlay Overlay { get; init; }
    public ITrackerService? TrackerService { get; set; }
    public TrackerServiceType? LastType { get; set; } = null;
    public IGameObject? ClosestMatch { get; private set; }
    public bool IsRaycasted { get; private set; } = false;
    public bool ErrorHooking { get; private set; } = false;
    public bool ErrorNoTobii { get; private set; } = false;
    public bool ErrorNoEyeware { get; private set; } = false;

    [Signature("E8 ?? ?? ?? FF 48 8D 8B ?? ?? 00 00 40 0F B6 D6 E8 ?? ?? ?? ?? 40 84 FF")]
    private readonly delegate* unmanaged<IntPtr, byte, void> HighlightGameObjectWithColorDelegate = null;


    [Signature("E8 ?? ?? ?? ?? 84 C0 44 8B C3")]
    private readonly delegate* unmanaged<InputManager*, int, bool> IsInputPressed = null;


    private delegate IntPtr SelectInitialTabTargetDelegate(IntPtr targetSystem, IntPtr gameObjects, IntPtr camera, IntPtr a4);

    [Signature("E8 ?? ?? ?? ?? EB 16 44 0F B6 CD", DetourName = nameof(SelectInitialTabTargetDetour))]
    private readonly Hook<SelectInitialTabTargetDelegate>? selectInitialTabTargetHook = null;


    private delegate IntPtr SelectTabTargetDelegate(IntPtr targetSystem, IntPtr camera, IntPtr gameObjects, bool inverse, IntPtr a5);

    [Signature("E8 ?? ?? ?? ?? EB 47 41 B1 01", DetourName = nameof(SelectTabTargetConeDetour))]
    private readonly Hook<SelectTabTargetDelegate>? selectTabTargetConeHook = null;

    [Signature("E8 ?? ?? ?? ?? 48 8B C8 48 85 C0 74 29", DetourName = nameof(SelectTabTargetIgnoreDepthDetour))]
    private readonly Hook<SelectTabTargetDelegate>? selectTabTargetIgnoreDepthHook = null;

    private unsafe bool IsCircleTargetInput()
    {
      var manager = InputManager.Instance();
      return IsInputPressed(manager, 18) || IsInputPressed(manager, 19);
    }

    private readonly Dictionary<IntPtr, float> gameObjectHeatMap = new();
    private nint tobiiGameIntegrationApix64Ptr = IntPtr.Zero;

    private nint eyewareTrackerClientApix64Ptr = IntPtr.Zero;

    public IDictionary<IntPtr, float> GameObjectHeatMap => gameObjectHeatMap;

    private bool NeedsOverwrite()
    {
      bool isEnemyTarget;
      if (IsCircleTargetInput())
      {
        isEnemyTarget = false;
      }
      else
      {
        isEnemyTarget = true;
      }

      var overwrite = false;
      if (Configuration.OverrideEnemyTarget && isEnemyTarget)
      {
        if (Configuration.OverrideEnemyTargetAlways || Service.TargetManager.Target == null)
        {
          overwrite = true;
        }
      }
      else if (Configuration.OverrideSoftTarget && !isEnemyTarget && (Configuration.OverrideSoftTargetAlways || Service.TargetManager.SoftTarget == null))
      {
        overwrite = true;
      }
      return overwrite;
    }

    private unsafe IntPtr SelectTabTargetIgnoreDepthDetour(IntPtr targetSystem, IntPtr camera, IntPtr gameObjects,
        bool inverse,
        IntPtr a5)
    {
      Service.PluginLog.Verbose($"SelectTabTargetIgnoreDepthDetour - {targetSystem:X} {camera:X} {gameObjects:X} {inverse} {a5:X}");
      var originalResult = selectTabTargetIgnoreDepthHook?.Original(targetSystem, camera, gameObjects, inverse, a5) ?? IntPtr.Zero;

      if (originalResult != IntPtr.Zero && Configuration.Enabled && ClosestMatch != null && NeedsOverwrite())
      {
        Service.PluginLog.Verbose($"SelectTabTargetIgnoreDepthDetour - Override tab target {originalResult:X} with {ClosestMatch.Address:X}");
        return ClosestMatch.Address;
      }
      return originalResult;
    }

    private IntPtr SelectTabTargetConeDetour(IntPtr targetSystem, IntPtr camera, IntPtr gameObjects, bool inverse,
        IntPtr a5)
    {
      Service.PluginLog.Verbose($"SelectTabTargetConeDetour - {targetSystem:X} {camera:X} {gameObjects:X} {inverse} {a5:X}");
      var originalResult = selectTabTargetConeHook?.Original(targetSystem, camera, gameObjects, inverse, a5) ?? IntPtr.Zero;

      if (originalResult != IntPtr.Zero && Configuration.Enabled && ClosestMatch != null && NeedsOverwrite())
      {
        Service.PluginLog.Verbose($"SelectTabTargetConeDetour - Override tab target {originalResult:X} with {ClosestMatch.Address:X}");
        return ClosestMatch.Address;
      }
      return originalResult;
    }

    private IntPtr SelectInitialTabTargetDetour(IntPtr targetSystem, IntPtr gameObjects, IntPtr camera, IntPtr a4)
    {
      Service.PluginLog.Verbose($"SelectInitialTabTargetDetour - {targetSystem:X} {gameObjects:X} {camera:X} {a4:X}");
      var originalResult = selectInitialTabTargetHook?.Original(targetSystem, gameObjects, camera, a4) ?? IntPtr.Zero;
      if (Configuration.Enabled && ClosestMatch != null && NeedsOverwrite())
      {
        Service.PluginLog.Verbose($"SelectInitialTabTargetDetour - Override tab target {originalResult:X} with {ClosestMatch.Address:X}");
        return ClosestMatch.Address;
      }
      return originalResult;
    }

    public string LocateNewestTobiiGameHub(string dirPath, Version? force = null)
    {
      var regex = new Regex(@"app-([0-9]+\.[0-9]+\.[0-9])-?.*");
      var dirs = Directory.GetDirectories(dirPath);
      var highestVersion = new Version(0, 0, 0);
      var highestVersionDir = "";
      foreach (var dir in dirs)
      {
        var dirName = Path.GetFileName(dir);
        var versPart = regex.Match(dirName).Groups[1].Value;
        if (Version.TryParse(versPart, out var version))
        {
          Service.PluginLog.Verbose($"Found Tobii GameHub version {version}");
          if (force == null && version > highestVersion)
          {
            highestVersion = version;
            highestVersionDir = dir;
          }
          else if (force != null && version == force)
          {
            return dir;
          }
        }
      }
      return highestVersionDir;
    }

    public MagitekStratagemPlugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager)
    {
      pluginInterface.Create<Service>();
      PluginInterface = pluginInterface;


      try
      {
        ResolveTobiiGameIntegrationDll();
      }
      catch (Exception ex)
      {
        Service.PluginLog.Error($"Error attempting to resolve Tobii Game Integration DLL's path: {ex.Message}");
        ErrorNoTobii = true;
      }

      try
      {
        ResolveEyewareDll();
      }
      catch (Exception ex)
      {
        Service.PluginLog.Error($"Error attempting to resolve Eyeware DLL's path: {ex.Message}");
        ErrorNoEyeware = true;
      }

      SetDllResolvers();

      CommandManager = commandManager;
      WindowSystem = new("MagitekStratagemPlugin");


      Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
      Configuration.Initialize(this);

      Random = new Random();

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

      try
      {
        Service.IGameInterop.InitializeFromAttributes(this);
      }
      catch (Exception ex)
      {
        Service.PluginLog.Error(ex.Message);
        ErrorHooking = true;
      }

      EnableHook(selectInitialTabTargetHook, "SelectInitialTabTarget");
      EnableHook(selectTabTargetConeHook, "SelectTabTargetCone");
      EnableHook(selectTabTargetIgnoreDepthHook, "SelectTabTargetIgnoreDepth");

      Service.Framework.Update += OnUpdate;
      PluginInterface.UiBuilder.Draw += DrawUI;
      PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
      PluginInterface.UiBuilder.OpenMainUi += DrawConfigUI;
    }

    private void ResolveTobiiGameIntegrationDll()
    {
      var tobiiDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TobiiGameHub");
      if (!Path.Exists(tobiiDir))
      {
        throw new TobiiGameHubNotFoundException();
      }
      var tobiiPath = LocateNewestTobiiGameHub(tobiiDir);

      if (tobiiPath != null)
      {
        Service.PluginLog.Verbose("Searching potential Tobii GameHub install path", tobiiPath);
        if (Path.Exists(tobiiPath))
        {
          var lib = Path.Join(tobiiPath, StreamEngine.Library);
          Service.PluginLog.Verbose("Loading Tobii Stream Engine DLL from " + lib);
          tobiiGameIntegrationApix64Ptr = NativeLibrary.Load(lib);
        }
        else
        {
          throw new TobiiGameIntegrationNotFoundException();
        }
      }
      else
      {
        Service.PluginLog.Verbose("Tobii GameHub not found.");
      }
    }

    private void ResolveEyewareDll()
    {
      Service.PluginLog.Verbose($"Searching for Eyeware Tracker Client DLL...");
      var pluginDir = PluginInterface.AssemblyLocation.Directory?.FullName;
      Service.PluginLog.Verbose("Plugin directory: " + pluginDir);

      if (pluginDir != null)
      {
        var lib = Path.Join(pluginDir, TrackerClient.Library);
        Service.PluginLog.Verbose("Loading Eyeware Tracker Client DLL from " + lib);
        eyewareTrackerClientApix64Ptr = NativeLibrary.Load(lib);
      }
      else
      {
        Service.PluginLog.Verbose("Eyeware Tracker Client DLL not found.");
      }
    }

    private void SetDllResolvers()
    {
      NativeLibrary.SetDllImportResolver(typeof(MagitekStratagemPlugin).Assembly, (libraryName, assembly, searchPath) =>
      {
        if (libraryName == StreamEngine.Library)
        {
          return this.tobiiGameIntegrationApix64Ptr;
        }
        if (libraryName == TrackerClient.Library)
        {
          return this.eyewareTrackerClientApix64Ptr;
        }
        return IntPtr.Zero;
      });
    }

    private void InitializeTrackerService()
    {
      try
      {
        if (Configuration.TrackerServiceType == TrackerServiceType.Tobii && LastType != TrackerServiceType.Tobii)
        {
          if (TrackerService != null)
          {
            TrackerService.StopTracking();
            TrackerService.Dispose();
          }
          LastType = TrackerServiceType.Tobii;
          TrackerService = new TobiiService();
          autoStarted = false;
        }
        else if (Configuration.TrackerServiceType == TrackerServiceType.BeamEye && LastType != TrackerServiceType.BeamEye)
        {
          if (TrackerService != null)
          {
            TrackerService.StopTracking();
            TrackerService.Dispose();
          }
          LastType = TrackerServiceType.BeamEye;
          TrackerService = new BeamService();
          autoStarted = false;
        }
        else if (Configuration.TrackerServiceType == TrackerServiceType.Fake && LastType != TrackerServiceType.Fake)
        {
          if (TrackerService != null)
          {
            TrackerService.StopTracking();
            TrackerService.Dispose();
          }
          LastType = TrackerServiceType.Fake;
          TrackerService = new FakeService();
          autoStarted = false;
        }
      }
      catch (Exception ex)
      {
        Service.PluginLog.Error(ex.Message);
        LastType = TrackerServiceType.Fake;
        TrackerService = new FakeService();
        autoStarted = false;
      }
    }

    private void EnableHook<T>(Hook<T>? hook, string hookName) where T : Delegate
    {
      if (hook != null)
      {
        Service.PluginLog.Verbose($"Successfully Hooked {hookName}");
        hook.Enable();
      }
      else
      {
        Service.PluginLog.Error($"Failed to hook {hookName}");
        ErrorHooking = true;
      }
    }

    public void Dispose()
    {
      TrackerService?.Dispose();
      TrackerService = null;

      unsafe
      {
        if (lastHighlightEntityId.HasValue && HighlightGameObjectWithColor != null)
        {
          var obj = FindGameObject(lastHighlightEntityId.Value);
          if (obj != null)
          {
            HighlightGameObjectWithColor(obj.Address, 0);
          }
          lastHighlightEntityId = null;
        }
      }

      selectInitialTabTargetHook?.Dispose();
      selectTabTargetConeHook?.Dispose();
      selectTabTargetIgnoreDepthHook?.Dispose();

      Service.Framework.Update -= OnUpdate;
      PluginInterface.UiBuilder.Draw -= DrawUI;
      PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
      PluginInterface.UiBuilder.OpenMainUi -= DrawConfigUI;

      WindowSystem.RemoveAllWindows();

      CommandManager.RemoveHandler(commandName);
      CommandManager.RemoveHandler(overlayCommandName);

    }

    private bool WatchingAnyCutscene()
    {
      return Service.Condition[ConditionFlag.OccupiedInCutSceneEvent] ||
        Service.Condition[ConditionFlag.WatchingCutscene] ||
        Service.Condition[ConditionFlag.OccupiedInEvent] ||
        Service.Condition[ConditionFlag.WatchingCutscene] ||
        Service.Condition[ConditionFlag.WatchingCutscene78];
    }

    private void HighlightGameObjectWithColor(IntPtr gameObject, byte color)
    {
      if (HighlightGameObjectWithColorDelegate == null || WatchingAnyCutscene())
      {
        return;
      }

      HighlightGameObjectWithColorDelegate(gameObject, color);
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
      TrackerService?.Draw();
    }

    private void DrawConfigUI()
    {
      SetVisible(!Configuration.IsVisible);
    }

    private unsafe IGameObject? FindGameObject(uint entityId)
    {
      var objectTable = Service.ObjectTable;
      var total = objectTable.Count();
      for (var i = 0; i < total; i++)
      {
        var obj = objectTable[i];
        if (obj == null)
        {
          continue;
        }
        if (obj.EntityId == entityId)
        {
          return obj;
        }
      }
      return null;
    }

    private IntPtr? FindMaxHeat()
    {
      if (gameObjectHeatMap.Keys.Count > 1)
      {
        return gameObjectHeatMap.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
      }
      else if (gameObjectHeatMap.Keys.Count == 1)
      {
        return gameObjectHeatMap.Keys.First();
      }
      else
      {
        return null;
      }
    }

    //Exponentially decay heat
    private void DecayHeat()
    {
      var keys = gameObjectHeatMap.Keys.ToList();
      foreach (var key in keys)
      {
        gameObjectHeatMap[key] = gameObjectHeatMap[key] * Configuration.HeatDecay;
        if (gameObjectHeatMap[key] < 0.01f)
        {
          gameObjectHeatMap.Remove(key);
        }
      }
    }

    public void OnUpdate(IFramework framework)
    {
      var player = Service.ClientState.LocalPlayer;
      IGameObject? lastHighlight = GetLastHighlight();

      InitializeTrackerService();

      if (Configuration.Enabled && TrackerService != null)
      {
        HandleTracking();

        if (TrackerService.IsTracking)
        {
          TrackerService.Update();

          if (Service.Condition.Any() && player != null && !WatchingAnyCutscene())
          {
            ProcessGaze(player, lastHighlight);
          }
        }
      }
      else
      {
        if (!WatchingAnyCutscene())
        {
          // TODO: Something is broken with this, it doesn't clear the last highlight correctly.
          ClearLastHighlight(lastHighlight);
        }
      }
    }

    private IGameObject? GetLastHighlight()
    {
      if (lastHighlightEntityId.HasValue)
      {
        return FindGameObject(lastHighlightEntityId.Value);
      }
      return null;
    }

    private void HandleTracking()
    {
      if (TrackerService != null && !TrackerService.IsTracking && Configuration.AutoStartTracking && !autoStarted)
      {
        TrackerService.StartTracking();
        autoStarted = true;
        Service.PluginLog.Debug("Tracking auto-start.");
      }
    }

    private void ProcessGaze(IGameObject player, IGameObject? lastHighlight)
    {
      unsafe
      {
        var size = ImGui.GetIO().DisplaySize;
        Vector2 gazeScreenPos = CalculateGazeScreenPos(size);
        if (Service.GameGui.ScreenToWorld(gazeScreenPos, out Vector3 worldPos))
        {
          UpdateClosestMatch(player, worldPos);
        }

        if (Configuration.UseRaycast && !IsUiFading())
        {
          PerformRaycasting(gazeScreenPos);
          UpdateClosestMatchFromHeatMap();
          DecayHeat();
        }

        UpdateHighlight(lastHighlight);
      }
    }

    private Vector2 CalculateGazeScreenPos(Vector2 size)
    {
      return new Vector2(
          TrackerService!.LastGazeX * (size.X / 2) + (size.X / 2),
          -TrackerService!.LastGazeY * (size.Y / 2) + (size.Y / 2));
    }

    private void UpdateClosestMatch(IGameObject player, Vector3 worldPos)
    {
      var closestDistance = float.MaxValue;
      foreach (var actor in Service.ObjectTable)
      {
        if (actor == null) continue;
        var gos = (GameObjectStruct*)actor.Address;
        if (gos->GetIsTargetable() && actor != player)
        {
          var distance = FFXIVClientStructs.FFXIV.Common.Math.Vector3.Distance(worldPos, actor.Position);
          if (ClosestMatch == null || closestDistance > distance)
          {
            ClosestMatch = actor;
            closestDistance = distance;
            IsRaycasted = false;
          }
        }
      }
    }

    private bool IsUiFading()
    {
      return RaptureAtkUnitManager.Instance()->IsUiFading;
    }

    private void PerformRaycasting(Vector2 gazeScreenPos)
    {
      for (var i = 0; i < Configuration.GazeCircleSegments + 1; i++)
      {
        var (rayPosX, rayPosY) = CalculateRayPosition(gazeScreenPos, i);
        var rayHit = TargetSystem.Instance()->GetMouseOverObject(rayPosX, rayPosY);
        UpdateHeatMap(rayHit);
      }
    }

    private (int rayPosX, int rayPosY) CalculateRayPosition(Vector2 gazeScreenPos, int i)
    {
      if (i == Configuration.GazeCircleSegments)
      {
        return ((int)gazeScreenPos.X, (int)gazeScreenPos.Y);
      }
      var randomFloat = (float)Random.NextDouble();
      return (
          (int)(gazeScreenPos.X + randomFloat * Configuration.GazeCircleRadius * Math.Cos(i * 2 * Math.PI / Configuration.GazeCircleSegments)),
          (int)(gazeScreenPos.Y + randomFloat * Configuration.GazeCircleRadius * Math.Sin(i * 2 * Math.PI / Configuration.GazeCircleSegments))
      );
    }

    private void UpdateHeatMap(GameObjectStruct* rayHit)
    {
      if (rayHit != null)
      {
        if (gameObjectHeatMap.ContainsKey((IntPtr)rayHit))
        {
          gameObjectHeatMap[(IntPtr)rayHit] += Configuration.HeatIncrement;
        }
        else
        {
          gameObjectHeatMap[(IntPtr)rayHit] = Configuration.HeatIncrement;
        }
      }
    }

    private void UpdateClosestMatchFromHeatMap()
    {
      var ptr = FindMaxHeat();
      if (ptr != null)
      {
        var raycasted = Service.ObjectTable.FirstOrDefault(x => (GameObjectStruct*)x.Address == (GameObjectStruct*)ptr);
        if (raycasted != null)
        {
          ClosestMatch = raycasted;
          IsRaycasted = true;
        }
      }
    }

    private void UpdateHighlight(IGameObject? lastHighlight)
    {
      if (ClosestMatch != null)
      {
        UpdateHighlightColor(lastHighlight);
        lastHighlightEntityId = ClosestMatch.EntityId;
      }
      else if (lastHighlight != null)
      {
        HighlightGameObjectWithColor(lastHighlight.Address, 0);
        lastHighlightEntityId = null;
      }
    }

    private void UpdateHighlightColor(IGameObject? lastHighlight)
    {
      if (ClosestMatch != null)
      {
        if (lastHighlight != null && lastHighlight.EntityId != ClosestMatch.EntityId)
        {
          HighlightGameObjectWithColor(lastHighlight.Address, 0);
          lastHighlightEntityId = null;
        }
        HighlightGameObjectWithColor(ClosestMatch.Address, IsRaycasted ? (byte)Configuration.HighlightColor : (byte)Configuration.ProximityColor);
      }
    }

    private void ClearLastHighlight(IGameObject? lastHighlight)
    {
      unsafe
      {
        if (lastHighlight != null)
        {
          HighlightGameObjectWithColor(lastHighlight.Address, 0);
          lastHighlightEntityId = null;
        }
      }
    }
  }
}
