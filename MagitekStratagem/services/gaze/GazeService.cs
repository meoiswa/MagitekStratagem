using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI;
using GameObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace MagitekStratagemPlugin
{
    public class GazeService : IDisposable
    {
        public Vector2 LastGazeScreenPos { get; private set; }
        public Vector3 LastGazeWorldPos { get; private set; }
        public IGameObject? ClosestMatch { get; private set; } = null;
        public IGameObject? LastHighlighted { get; private set; }
        public bool IsRaycasted { get; private set; }

        private readonly Random random;
        private readonly GameObjectHeatmapService heatmapService;
        private readonly Configuration configuration;

        public GazeService(
            GameObjectHeatmapService heatmapService,
            Configuration configuration)
        {
            this.random = new Random();
            this.heatmapService = heatmapService;
            this.configuration = configuration;
        }

        public void Update(IGameObject? player, TrackerService? activeTracker)
        {
            if (configuration.Enabled && activeTracker != null)
            {
                if (activeTracker.IsTracking)
                {
                    var size = ImGui.GetIO().DisplaySize;
                    LastGazeScreenPos = CalculateGazeScreenPos(size, activeTracker.LastGazePos);

                    if (Service.Condition.Any() && player != null && !WatchingAnyCutscene())
                    {
                        ProcessGaze(player, LastGazeScreenPos);
                    }
                }
            }
            else
            {
                ClosestMatch = null;
                IsRaycasted = false;
                UpdateHighlight();
            }
        }

        public void Dispose()
        {
            UpdateHighlight();
        }

        private bool WatchingAnyCutscene()
        {
            return Service.Condition[ConditionFlag.OccupiedInCutSceneEvent] ||
              Service.Condition[ConditionFlag.WatchingCutscene] ||
              Service.Condition[ConditionFlag.OccupiedInEvent] ||
              Service.Condition[ConditionFlag.WatchingCutscene] ||
              Service.Condition[ConditionFlag.WatchingCutscene78];
        }

        private void ProcessGaze(IGameObject player, Vector2 screenGazePos)
        {
            if (Service.GameGui.ScreenToWorld(screenGazePos, out Vector3 worldPos))
            {
                LastGazeWorldPos = worldPos;
                UpdateClosestMatch(player, worldPos);
            }

            if (configuration.UseRaycast && !IsUiFading())
            {
                PerformRaycasting(screenGazePos);
                UpdateClosestMatchFromHeatMap();
                heatmapService.DecayHeat(configuration.HeatDecay);
            }

            UpdateHighlight();
        }

        private unsafe void PerformRaycasting(Vector2 gazeScreenPos)
        {
            for (var i = 0; i < configuration.GazeCircleSegments + 1; i++)
            {
                var (rayPosX, rayPosY) = CalculateRayPosition(gazeScreenPos, i);
                var rayHit = TargetSystem.Instance()->GetMouseOverObject(rayPosX, rayPosY);
                heatmapService.UpdateHeatMap(rayHit);
            }
        }

        private (int rayPosX, int rayPosY) CalculateRayPosition(Vector2 gazeScreenPos, int i)
        {
            if (i == configuration.GazeCircleSegments)
            {
                return ((int)gazeScreenPos.X, (int)gazeScreenPos.Y);
            }
            var randomFloat = (float)random.NextDouble();
            return (
                (int)(gazeScreenPos.X + randomFloat * configuration.GazeCircleRadius * Math.Cos(i * 2 * Math.PI / configuration.GazeCircleSegments)),
                (int)(gazeScreenPos.Y + randomFloat * configuration.GazeCircleRadius * Math.Sin(i * 2 * Math.PI / configuration.GazeCircleSegments))
            );
        }

        private unsafe bool IsUiFading()
        {
            return RaptureAtkUnitManager.Instance()->IsUiFading;
        }

        private Vector2 CalculateGazeScreenPos(Vector2 screenSize, Vector2 trackerGazePos = default)
        {
            return new Vector2(
                trackerGazePos.X * (screenSize.X / 2) + (screenSize.X / 2),
                -trackerGazePos.Y * (screenSize.Y / 2) + (screenSize.Y / 2));
        }

        private unsafe void UpdateClosestMatch(IGameObject player, Vector3 worldPos)
        {
            var closestDistance = float.MaxValue;
            foreach (var actor in Service.ObjectTable)
            {
                if (actor == null) continue;
                var gos = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)actor.Address;
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

        private void UpdateClosestMatchFromHeatMap()
        {
            if (heatmapService != null)
            {
                var max = heatmapService.FindMaxHeat();
                if (max != null)
                {
                    ClosestMatch = Service.ObjectTable.FirstOrDefault(x => x != null && x.Address == max);
                    IsRaycasted = true;
                }
            }
        }

        private unsafe void UpdateHighlight()
        {
            var lastHighlight = LastHighlighted;

            if (ClosestMatch != null)
            {
                if (lastHighlight != null && ClosestMatch != lastHighlight)
                {
                    ((GameObjectStruct*)lastHighlight.Address)->Highlight(0);
                }

                ((GameObjectStruct*)ClosestMatch.Address)->Highlight(IsRaycasted ? configuration.HighlightColor : configuration.ProximityColor);
                LastHighlighted = ClosestMatch;
            }
            else
            {
                if (lastHighlight != null)
                {
                    ((GameObjectStruct*)lastHighlight.Address)->Highlight(0);
                    LastHighlighted = null;
                }
            }
        }
    }
}
