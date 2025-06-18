using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace MagitekStratagemPlugin
{
    public unsafe class AddonService
    {
        private delegate void* MoveAddon(RaptureAtkModule* atkModule, AtkUnitBase* addon, void* idk);

        [Signature("40 53 48 83 EC 20 80 A2", DetourName = nameof(MoveAddonDetour))]
        private Hook<MoveAddon>? moveAddonHook;

        public GazeService GazeService { get; }

        public AddonService(GazeService gazeService)
        {
            this.GazeService = gazeService;
        }

        public void EnableHooks()
        {
            if (moveAddonHook != null)
            {
                Service.PluginLog.Information("Enabling Addon Hooks...");
                moveAddonHook.Enable();
                Service.PluginLog.Verbose("Successfully Hooked MoveAddon");
            }
            else
            {
                Service.PluginLog.Error("Failed to hook MoveAddon");
            }
        }

        private void* MoveAddonDetour(RaptureAtkModule* atkModule, AtkUnitBase* addon, void* idk)
        {
            if (moveAddonHook == null)
            {
                return default;
            }

            try
            {
                var name = addon->NameString;
                var x = addon->X;
                var y = addon->Y;
                var width = addon->WindowNode->Width;
                var height = addon->WindowNode->Height;

                Service.PluginLog.Verbose($"MoveAddonDetour called for {name} at ({x}, {y}) with size ({width}, {height})");
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error($"Error in MoveAddonDetour", ex);
            }


            return moveAddonHook.Original(atkModule, addon, idk);
        }
    }
}
