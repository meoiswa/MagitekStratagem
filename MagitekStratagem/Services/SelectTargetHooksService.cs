using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace MagitekStratagemPlugin
{
    public unsafe class SelectTargetHooksService : IDisposable
    {
        private readonly MagitekStratagemPlugin plugin;
        
        private delegate IntPtr SelectInitialTabTargetDelegate(IntPtr targetSystem, IntPtr gameObjects, IntPtr camera, IntPtr a4);
        private delegate IntPtr SelectTabTargetDelegate(IntPtr targetSystem, IntPtr camera, IntPtr gameObjects, bool inverse, IntPtr a5);

        [Signature("E8 ?? ?? ?? ?? EB 11 44 0F B6 CD", DetourName = nameof(SelectInitialTabTargetDetour))]
        private readonly Hook<SelectInitialTabTargetDelegate>? selectInitialTabTargetHook = null;

        [Signature("E8 ?? ?? ?? ?? EB 4C 41 B1 01", DetourName = nameof(SelectTabTargetConeDetour))]
        private readonly Hook<SelectTabTargetDelegate>? selectTabTargetConeHook = null;

        [Signature("E8 ?? ?? ?? ?? 48 8B C8 48 85 C0 74 29", DetourName = nameof(SelectTabTargetIgnoreDepthDetour))]
        private readonly Hook<SelectTabTargetDelegate>? selectTabTargetIgnoreDepthHook = null;

        [Signature("E8 ?? ?? ?? ?? 84 C0 44 8B C3")]
        private readonly delegate* unmanaged<InputManager*, int, bool> IsInputPressed = null;

        public SelectTargetHooksService(MagitekStratagemPlugin plugin)
        {
            this.plugin = plugin;
        }

        public void EnableHooks()
        {
            Service.PluginLog.Information("Enabling SelectTarget Hooks...");
            EnableHook(selectInitialTabTargetHook, "SelectInitialTabTarget");
            EnableHook(selectTabTargetConeHook, "SelectTabTargetCone");
            EnableHook(selectTabTargetIgnoreDepthHook, "SelectTabTargetIgnoreDepth");

            if (IsInputPressed == null)
            {
                Service.PluginLog.Error("Failed to hook IsInputPressed");
                plugin.ErrorHooking = true;
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
                plugin.ErrorHooking = true;
            }
        }

        private unsafe bool IsCircleTargetInput()
        {
            var manager = InputManager.Instance();
            return IsInputPressed(manager, 18) || IsInputPressed(manager, 19);
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
            if (plugin.Configuration.OverrideEnemyTarget && isEnemyTarget)
            {
                if (plugin.Configuration.OverrideEnemyTargetAlways || Service.TargetManager.Target == null)
                {
                    overwrite = true;
                }
            }
            else if (plugin.Configuration.OverrideSoftTarget && !isEnemyTarget && (plugin.Configuration.OverrideSoftTargetAlways || Service.TargetManager.SoftTarget == null))
            {
                overwrite = true;
            }
            return overwrite;
        }

        private IntPtr SelectInitialTabTargetDetour(IntPtr targetSystem, IntPtr gameObjects, IntPtr camera, IntPtr a4)
        {
            Service.PluginLog.Verbose($"SelectInitialTabTargetDetour - {targetSystem:X} {gameObjects:X} {camera:X} {a4:X}");
            var originalResult = selectInitialTabTargetHook?.Original(targetSystem, gameObjects, camera, a4) ?? IntPtr.Zero;
            if (plugin.Configuration.Enabled && plugin.GazeService.ClosestMatch != null && NeedsOverwrite())
            {
                Service.PluginLog.Verbose($"SelectInitialTabTargetDetour - Override tab target {originalResult:X} with {plugin.GazeService.ClosestMatch.Address:X}");
                return plugin.GazeService.ClosestMatch.Address;
            }
            return originalResult;
        }

        private IntPtr SelectTabTargetConeDetour(IntPtr targetSystem, IntPtr camera, IntPtr gameObjects, bool inverse, IntPtr a5)
        {
            Service.PluginLog.Verbose($"SelectTabTargetConeDetour - {targetSystem:X} {camera:X} {gameObjects:X} {inverse} {a5:X}");
            var originalResult = selectTabTargetConeHook?.Original(targetSystem, camera, gameObjects, inverse, a5) ?? IntPtr.Zero;
            if (originalResult != IntPtr.Zero && plugin.Configuration.Enabled && plugin.GazeService.ClosestMatch != null && NeedsOverwrite())
            {
                Service.PluginLog.Verbose($"SelectTabTargetConeDetour - Override tab target {originalResult:X} with {plugin.GazeService.ClosestMatch.Address:X}");
                return plugin.GazeService.ClosestMatch.Address;
            }
            return originalResult;
        }

        private IntPtr SelectTabTargetIgnoreDepthDetour(IntPtr targetSystem, IntPtr camera, IntPtr gameObjects, bool inverse, IntPtr a5)
        {
            Service.PluginLog.Verbose($"SelectTabTargetIgnoreDepthDetour - {targetSystem:X} {camera:X} {gameObjects:X} {inverse} {a5:X}");
            var originalResult = selectTabTargetIgnoreDepthHook?.Original(targetSystem, camera, gameObjects, inverse, a5) ?? IntPtr.Zero;
            if (originalResult != IntPtr.Zero && plugin.Configuration.Enabled && plugin.GazeService.ClosestMatch != null && NeedsOverwrite())
            {
                Service.PluginLog.Verbose($"SelectTabTargetIgnoreDepthDetour - Override tab target {originalResult:X} with {plugin.GazeService.ClosestMatch.Address:X}");
                return plugin.GazeService.ClosestMatch.Address;
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
