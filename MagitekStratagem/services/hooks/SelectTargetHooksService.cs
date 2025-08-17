using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace MagitekStratagemPlugin
{

    public unsafe class SelectTargetHooksService : IDisposable
    {
        public bool ErrorHooking { get; private set; } = false;
        public Configuration Configuration { get; }
        public GazeService GazeService { get; }
        public bool SelectInitialTabTargetHooked => selectInitialTabTargetHook != null;
        public bool SelectTabTargetConeHooked => selectTabTargetConeHook != null;
        public bool SelectTabTargetIgnoreDepthHooked => selectTabTargetIgnoreDepthHook != null;
        public bool GetInputStatusHooked => GetInputStatus != null;

        private delegate IntPtr SelectInitialTabTargetDelegate(IntPtr targetSystem, IntPtr gameObjects, IntPtr camera, IntPtr a4);
        private delegate IntPtr SelectTabTargetDelegate(IntPtr targetSystem, IntPtr camera, IntPtr gameObjects, bool inverse, IntPtr a5);

        [Signature("E8 ?? ?? ?? ?? EB 11 44 0F B6 CD", DetourName = nameof(SelectInitialTabTargetDetour))]
        private readonly Hook<SelectInitialTabTargetDelegate>? selectInitialTabTargetHook = null;

        [Signature("E8 ?? ?? ?? ?? EB ?? 41 B1 ?? 48 8D 54 24", DetourName = nameof(SelectTabTargetConeDetour))]
        private readonly Hook<SelectTabTargetDelegate>? selectTabTargetConeHook = null;

        [Signature("E8 ?? ?? ?? ?? 48 8B AC 24 ?? ?? ?? ?? 48 8B D8 48 85 C0", DetourName = nameof(SelectTabTargetIgnoreDepthDetour))]
        private readonly Hook<SelectTabTargetDelegate>? selectTabTargetIgnoreDepthHook = null;

        [Signature("E8 ?? ?? ?? ?? 84 C0 44 8B C3")]
        private readonly delegate* unmanaged<InputManager*, int, bool> GetInputStatus = null;

        public SelectTargetHooksService(GazeService gazeService, Configuration configuration)
        {
            Configuration = configuration;
            GazeService = gazeService;

            try
            {
                Service.GameInterop.InitializeFromAttributes(this);
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error(ex.Message);
                ErrorHooking = true;
            }
        }

        public void EnableHooks()
        {
            if (!ErrorHooking)
            {
                Service.PluginLog.Information("Enabling SelectTarget Hooks...");
                EnableHook(selectInitialTabTargetHook, "SelectInitialTabTarget");
                EnableHook(selectTabTargetConeHook, "SelectTabTargetCone");
                EnableHook(selectTabTargetIgnoreDepthHook, "SelectTabTargetIgnoreDepth");

                if (GetInputStatus == null)
                {
                    Service.PluginLog.Error("Failed to hook GetInputStatus");
                    ErrorHooking = true;
                }
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

        private unsafe bool IsCircleTargetInput()
        {
            var manager = InputManager.Instance();

            // If 16 is false, it indicates a keyboard circle target:
            if (!GetInputStatus(manager, 16))
            {
                Service.PluginLog.Verbose("IsCircleTargetInput - Detected keyboard circle target");
                return true;
            }

            // Else if 18 XOR 19 is pressed, it's also a circle target
            if (GetInputStatus(manager, 18) ^ GetInputStatus(manager, 19))
            {
                Service.PluginLog.Verbose("IsCircleTargetInput - Detected gamepad circle target");
                return true;
            }

            return false;
        }

        private bool NeedsOverwrite()
        {
            Service.PluginLog.Verbose("NeedsOverwrite");
            bool isEnemyTarget;
            if (IsCircleTargetInput())
            {
                Service.PluginLog.Verbose("Circle target input detected");
                isEnemyTarget = false;
            }
            else
            {
                Service.PluginLog.Verbose("Tab target input detected");
                isEnemyTarget = true;
            }

            var overwrite = false;
            if (Configuration.OverrideEnemyTarget && isEnemyTarget)
            {
                Service.PluginLog.Verbose($"Service.TargetManager.Target: {Service.TargetManager.Target?.Name}");
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

        private IntPtr SelectInitialTabTargetDetour(IntPtr targetSystem, IntPtr gameObjects, IntPtr camera, IntPtr a4)
        {
            Service.PluginLog.Verbose($"SelectInitialTabTargetDetour - {targetSystem:X} {gameObjects:X} {camera:X} {a4:X}");
            var originalResult = selectInitialTabTargetHook?.Original(targetSystem, gameObjects, camera, a4) ?? IntPtr.Zero;
            if (Configuration.Enabled && GazeService.ClosestMatch != null && NeedsOverwrite())
            {
                Service.PluginLog.Verbose($"SelectInitialTabTargetDetour - Override tab target {originalResult:X} with {GazeService.ClosestMatch.Address:X}");
                return GazeService.ClosestMatch.Address;
            }
            return originalResult;
        }

        private IntPtr SelectTabTargetConeDetour(IntPtr targetSystem, IntPtr camera, IntPtr gameObjects, bool inverse, IntPtr a5)
        {
            Service.PluginLog.Verbose($"SelectTabTargetConeDetour - {targetSystem:X} {camera:X} {gameObjects:X} {inverse} {a5:X}");
            var originalResult = selectTabTargetConeHook?.Original(targetSystem, camera, gameObjects, inverse, a5) ?? IntPtr.Zero;
            if (originalResult != IntPtr.Zero && Configuration.Enabled && GazeService.ClosestMatch != null && NeedsOverwrite())
            {
                Service.PluginLog.Verbose($"SelectTabTargetConeDetour - Override tab target {originalResult:X} with {GazeService.ClosestMatch.Address:X}");
                return GazeService.ClosestMatch.Address;
            }
            return originalResult;
        }

        private IntPtr SelectTabTargetIgnoreDepthDetour(IntPtr targetSystem, IntPtr camera, IntPtr gameObjects, bool inverse, IntPtr a5)
        {
            Service.PluginLog.Verbose($"SelectTabTargetIgnoreDepthDetour - {targetSystem:X} {camera:X} {gameObjects:X} {inverse} {a5:X}");
            var originalResult = selectTabTargetIgnoreDepthHook?.Original(targetSystem, camera, gameObjects, inverse, a5) ?? IntPtr.Zero;
            if (originalResult != IntPtr.Zero && Configuration.Enabled && GazeService.ClosestMatch != null && NeedsOverwrite())
            {
                Service.PluginLog.Verbose($"SelectTabTargetIgnoreDepthDetour - Override tab target {originalResult:X} with {GazeService.ClosestMatch.Address:X}");
                return GazeService.ClosestMatch.Address;
            }
            return originalResult;
        }

        public void Dispose()
        {
            selectInitialTabTargetHook?.Dispose();
            selectTabTargetConeHook?.Dispose();
            selectTabTargetIgnoreDepthHook?.Dispose();
        }
    }
}
