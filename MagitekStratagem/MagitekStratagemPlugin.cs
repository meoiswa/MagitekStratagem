using System;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Newtonsoft.Json;
using Dalamud.Game.ClientState.Objects.Types;
using GameObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using TargetSystemStruct = FFXIVClientStructs.FFXIV.Client.Game.Control.TargetSystem;
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

namespace MagitekStratagemPlugin
{
  public sealed unsafe class MagitekStratagemPlugin : IDalamudPlugin
  {
    public string Name => "Magitek Stratagem";

    private const string commandName = "/magiteks";
    private const string overlayCommandName = "/magiteksoverlay";

    private unsafe uint? lastHighlightGameObjectId = null;

    private bool autoStarted;

    public DalamudPluginInterface PluginInterface { get; init; }
    public ICommandManager CommandManager { get; init; }
    public Configuration Configuration { get; init; }
    public WindowSystem WindowSystem { get; init; }
    public Random Random { get; }
    public MagitekStratagemUI Window { get; init; }
    public MagitekStratagemOverlay Overlay { get; init; }
    public ITrackerService? TrackerService { get; set; }
    public GameObject? ClosestMatch { get; private set; }
    public bool IsRaycasted { get; private set; } = false;
    public bool ErrorHooking { get; private set; } = false;
    public bool IsCalibrationEditMode { get; set; }

    [Signature("E8 ?? ?? ?? FF 48 8D 8B ?? ?? 00 00 40 0F B6 D6 E8 ?? ?? ?? ?? 40 84 FF")]
    private readonly delegate* unmanaged<IntPtr, byte, void> HighlightGameObjectWithColor = null;


    [Signature("E8 ?? ?? ?? ?? 3C 01 74 38")]
    private readonly delegate* unmanaged<InputManager*, int, bool> IsInputClicked = null;


    private delegate IntPtr SelectInitialTabTargetDelegate(IntPtr targetSystem, IntPtr gameObjects, IntPtr camera, IntPtr a4);

    [Signature("E8 ?? ?? ?? ?? EB 37 48 85 C9", DetourName = nameof(SelectInitialTabTargetDetour))]
    private readonly Hook<SelectInitialTabTargetDelegate>? selectInitialTabTargetHook = null;


    private delegate IntPtr SelectTabTargetDelegate(IntPtr targetSystem, IntPtr camera, IntPtr gameObjects, bool inverse, IntPtr a5);

    [Signature("E8 ?? ?? ?? ?? EB 4C 41 B1 01", DetourName = nameof(SelectTabTargetConeDetour))]
    private readonly Hook<SelectTabTargetDelegate>? selectTabTargetConeHook = null;

    [Signature("E8 ?? ?? ?? ?? 48 8B C8 48 85 C0 74 27 48 8B 00", DetourName = nameof(SelectTabTargetIgnoreDepthDetour))]
    private readonly Hook<SelectTabTargetDelegate>? selectTabTargetIgnoreDepthHook = null;


    public readonly Dictionary<IntPtr, float> gameObjectHeatMap = new();

    private unsafe bool IsCircleTargetInput()
    {
      var manager = (InputManager*)Service.SigScanner.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 0F 28 F0 45 0F 57 C0");
      return IsInputClicked(manager, 18) || IsInputClicked(manager, 19);
    }

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
      else if (Configuration.OverrideSoftTarget && !isEnemyTarget)
      {
        if (Configuration.OverrideSoftTargetAlways || Service.TargetManager.SoftTarget == null)
        {
          overwrite = true;
        }
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

    public string LocateNewestTobiiGameHub(string dirPath)
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
          Console.WriteLine(version);
          if (version > highestVersion)
          {
            highestVersion = version;
            highestVersionDir = dir;
          }
        }
      }
      return highestVersionDir;
    }

    public MagitekStratagemPlugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] ICommandManager commandManager)
    {
      NativeLibrary.SetDllImportResolver(typeof(MagitekStratagemPlugin).Assembly, (libraryName, assembly, searchPath) =>
      {
        var tobiiDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TobiiGameHub");
        if (!Path.Exists(tobiiDir))
        {
          throw new Exception("Tobii Game Hub not found.");
        }

        var tobiiPath = LocateNewestTobiiGameHub(tobiiDir);

        Service.PluginLog.Verbose("Searching potential Tobii GameHub install path", tobiiPath);
        if (Path.Exists(tobiiPath))
        {
          if (libraryName == "tobii_gameintegration_x64.dll")
          {
            var lib = Path.Join(tobiiPath, "tobii_gameintegration_x64.dll");
            Service.PluginLog.Verbose("Loading Tobii Game Integration DLL from " + lib);
            return NativeLibrary.Load(lib);
          }
          else if (libraryName == "tobii_gameintegration_x86.dll")
          {
            var lib = Path.Join(tobiiPath, "tobii_gameintegration_x86.dll");
            Service.PluginLog.Verbose("Loading Tobii Game Integration DLL from " + lib);
            return NativeLibrary.Load(lib);
          }
        }
        else
        {
          throw new Exception("Something went wrong locating the newest version of Tobii Game Hub.");
        }

        return IntPtr.Zero;
      });

      pluginInterface.Create<Service>();

      PluginInterface = pluginInterface;
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
        TrackerService = new TobiiService(Configuration.CalibrationPoints);
      }
      catch (Exception ex)
      {
        Service.PluginLog.Error(ex.Message);
      }

      Service.IGameInterop.InitializeFromAttributes(this);

      if (selectInitialTabTargetHook != null)
      {
        Service.PluginLog.Verbose("Successfully Hooked SelectInitialTabTarget");
        selectInitialTabTargetHook.Enable();
      }
      else
      {
        Service.PluginLog.Error("Failed to hook SelectInitialTabTarget");
        ErrorHooking = true;
      }

      if (selectTabTargetConeHook != null)
      {
        Service.PluginLog.Verbose("Successfully Hooked SelectTabTargetCone");
        selectTabTargetConeHook.Enable();
      }
      else
      {
        Service.PluginLog.Error("Failed to hook SelectTabTargetCone");
        ErrorHooking = true;
      }

      if (selectTabTargetIgnoreDepthHook != null)
      {
        Service.PluginLog.Verbose("Successfully Hooked SelectTabTargetIgnoreDepth");
        selectTabTargetIgnoreDepthHook.Enable();
      }
      else
      {
        Service.PluginLog.Error("Failed to hook SelectTabTargetIgnoreDepth");
        ErrorHooking = true;
      }

      Service.Framework.Update += OnUpdate;
      PluginInterface.UiBuilder.Draw += DrawUI;
      PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
    }

    public void Dispose()
    {
      unsafe
      {
        if (lastHighlightGameObjectId.HasValue && HighlightGameObjectWithColor != null)
        {
          var obj = FindGameObject(lastHighlightGameObjectId.Value);
          if (obj != null)
          {
            HighlightGameObjectWithColor(obj.Address, 0);
          }
          lastHighlightGameObjectId = null;
        }
      }

      selectInitialTabTargetHook?.Dispose();
      selectTabTargetConeHook?.Dispose();
      selectTabTargetIgnoreDepthHook?.Dispose();

      Service.Framework.Update -= OnUpdate;
      PluginInterface.UiBuilder.Draw -= DrawUI;
      PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;

      WindowSystem.RemoveAllWindows();

      CommandManager.RemoveHandler(commandName);
      CommandManager.RemoveHandler(overlayCommandName);

      TrackerService?.Shutdown();
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

    private unsafe GameObject? FindGameObject(uint objectId)
    {
      var objectTable = Service.ObjectTable;
      for (var i = 0; i < objectTable.Count; i++)
      {
        var obj = objectTable[i];
        if (obj == null)
        {
          continue;
        }
        if (obj.ObjectId == objectId)
        {
          return obj;
        }
      }
      return null;
    }

    private unsafe GameObjectStruct* GetMouseOverObject(int x, int y)
    {
      var ObjectFilterArray1Ptr = (GameObjectArray*)((IntPtr)TargetSystem.Instance() + 0x1a98);
      var ObjectFilterArray1 = *ObjectFilterArray1Ptr;
      var camera = Control.Instance()->CameraManager.Camera;
      var localPlayer = Control.Instance()->LocalPlayer;
      if (!Service.Condition.Any() || camera == null || localPlayer == null || ObjectFilterArray1.Length <= 0)
        return null;
      if (TargetSystem.Instance() != null && TargetSystem.Instance() != default(TargetSystemStruct*))
      {
        return TargetSystem.Instance()->GetMouseOverObject(x, y, ObjectFilterArray1Ptr, camera);
      }
      else
      {
        return null;
      }
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
      if (ErrorHooking)
      {
        return;
      }

      if (selectTabTargetConeHook == null || selectTabTargetIgnoreDepthHook == null || selectInitialTabTargetHook == null || HighlightGameObjectWithColor == null)
      {
        ErrorHooking = true;
        return;
      }



      var player = Service.ClientState.LocalPlayer;

      GameObject? lastHighlight = null;
      if (lastHighlightGameObjectId.HasValue)
      {
        lastHighlight = FindGameObject(lastHighlightGameObjectId.Value);
      }

      if (Configuration.Enabled && TrackerService != null)
      {
        if (!TrackerService.IsTracking)
        {
          if (Configuration.AutoStartTracking && !autoStarted)
          {
            TrackerService.StartTrackingWindow(PluginInterface.UiBuilder.WindowHandlePtr);
            autoStarted = true;
            Service.PluginLog.Debug("Tobii Eye Tracking auto-start.");
          }
          return;
        }

        var couldReadConfig = Service.IGameConfig.TryGet(SystemConfigOption.ScreenMode, out uint mode);
        if (!couldReadConfig || mode == 0)
        {
          TrackerService.UseCalibration = false;
        }
        else if (couldReadConfig && mode != 0)
        {
          TrackerService.UseCalibration = Configuration.UseCalibration;
        }


        TrackerService.Update();

        if (Service.Condition.Any() && player != null && TrackerService != null)
        {
          unsafe
          {
            var size = ImGui.GetIO().DisplaySize;
            Vector2 gazeScreenPos = new Vector2(
             TrackerService.LastGazeX * (size.X / 2) + (size.X / 2),
             -TrackerService.LastGazeY * (size.Y / 2) + (size.Y / 2));

            if (Service.GameGui.ScreenToWorld(gazeScreenPos, out Vector3 worldPos))
            {
              var closestDistance = float.MaxValue;

              foreach (var actor in Service.ObjectTable)
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
                    IsRaycasted = false;
                  }
                }
              }
            }

            if (Configuration.UseRaycast)
            {
              for (var i = 0; i < Configuration.GazeCircleSegments + 1; i++)
              {
                int rayPosX, rayPosY;
                if (i == Configuration.GazeCircleSegments)
                {
                  // Casta  ray exactly at the center of the gaze point
                  rayPosX = (int)gazeScreenPos.X;
                  rayPosY = (int)gazeScreenPos.Y;
                }
                else
                {
                  // Cast a ray in a circle around the gaze point, with a randomized offset
                  var randomFloat = (float)Random.NextDouble();
                  rayPosX = (int)(gazeScreenPos.X + (randomFloat * Configuration.GazeCircleRadius * Math.Cos(i * 2 * Math.PI / Configuration.GazeCircleSegments)));
                  rayPosY = (int)(gazeScreenPos.Y + (randomFloat * Configuration.GazeCircleRadius * Math.Sin(i * 2 * Math.PI / Configuration.GazeCircleSegments)));
                }
                var rayHit = GetMouseOverObject(rayPosX, rayPosY);
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

              DecayHeat();
            }

            if (ClosestMatch != null)
            {
              if (lastHighlight != null && lastHighlight.ObjectId != ClosestMatch.ObjectId)
              {
                HighlightGameObjectWithColor(lastHighlight.Address, 0);
                lastHighlightGameObjectId = null;
              }

              HighlightGameObjectWithColor(ClosestMatch.Address, IsRaycasted ? (byte)Configuration.HighlightColor : (byte)Configuration.ProximityColor);
              lastHighlightGameObjectId = ClosestMatch.ObjectId;
            }
            else if (ClosestMatch == null && lastHighlight != null)
            {
              HighlightGameObjectWithColor(lastHighlight.Address, 0);
              lastHighlightGameObjectId = null;
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
            HighlightGameObjectWithColor(lastHighlight.Address, 0);
            lastHighlightGameObjectId = null;
          }
        }
      }
    }
  }
}
