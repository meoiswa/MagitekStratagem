using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace MagitekStratagemPlugin
{
    public unsafe class AddonService
    {
        private Dictionary<string, byte> originalAddonAlphas = new();

        public GazeService GazeService { get; init; }

        public Configuration Configuration { get; init; }

        public AddonService(GazeService gazeService, Configuration configuration)
        {
            GazeService = gazeService;
            Configuration = configuration;
        }

        public void Dispose()
        {
        }

        public void Update()
        {
            try
            {
                CheckAllAddons();
            }
            catch (Exception ex)
            {
                Service.PluginLog.Error($"Error in AddonService.OnFrameworkUpdate", ex);
            }
        }

        private void CheckAllAddons()
        {
            var raptureAtkModule = RaptureAtkModule.Instance();
            if (raptureAtkModule == null)
            {
                return;
            }

            var unitManager = raptureAtkModule->AtkUnitManager;
            if (unitManager == null)
            {
                return;
            }

            var unitList = unitManager->AllLoadedUnitsList;

            for (int i = 0; i < unitList.Count; i++)
            {
                var addon = unitList.Entries[i].Value;
                if (addon == null || addon->WindowNode == null || !addon->IsVisible)
                {
                    continue;
                }

                var name = addon->NameString;
                var windowTopLeft = new Vector2(addon->X, addon->Y);
                var windowTopRight = new Vector2(addon->X + addon->WindowNode->Width, addon->Y + addon->WindowNode->Height);
                var gazePos = GazeService.LastGazeScreenPos;
                var radius = Configuration.GazeCircleRadius;

                if (!originalAddonAlphas.ContainsKey(name))
                {
                    originalAddonAlphas[name] = addon->Alpha;
                }

                if (gazePos.X + radius > windowTopLeft.X && gazePos.X - radius < windowTopRight.X &&
                    gazePos.Y + radius > windowTopLeft.Y && gazePos.Y - radius < windowTopRight.Y)
                {
                    // Gaze is within the addon, restore its original Alpha
                    addon->Alpha = originalAddonAlphas[name];
                }
                else
                {
                    // Gaze is outside the addon, set its Alpha to Configuration.AddonOutOfGazeAlpha
                    addon->Alpha = Configuration.AddonOutOfGazeAlpha;
                }
            }
        }
    }
}
