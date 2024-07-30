using System;
using System.ComponentModel;
using System.Diagnostics;
using MagitekStratagemPlugin.Eyeware;

namespace MagitekStratagemPlugin.Eyeware
{
    public sealed class BeamService : ITrackerService
    {
        public bool IsTracking => trackerClient?.Connected() ?? false;
        public long LastGazeTimestamp { get; private set; }
        public float LastGazeX { get; private set; }
        public float LastGazeY { get; private set; }

        private TrackerClient? trackerClient;

        public BeamService()
        {
            Service.PluginLog.Info("Eyeware Beam Service Initialized");
        }

        public void StartTracking()
        {
            if (trackerClient == null)
            {
                try
                {
                    trackerClient = new TrackerClient();
                    Service.PluginLog.Verbose("Eyeware Beam Tracker Client Initialized");
                }
                catch (Exception e)
                {
                    Service.PluginLog.Error(e.Message);
                }
            }
        }

        public void StopTracking()
        {
            trackerClient?.Dispose();
            trackerClient = null;
        }

        public void Update()
        {
            if (trackerClient == null)
            {
                return;
            }

            if (trackerClient.Connected())
            {
                var gazeInfo = trackerClient.GetScreenGazeInfo();
                LastGazeTimestamp = DateTime.Now.Ticks;

                unsafe
                {
                    var size = ImGuiNET.ImGui.GetIO().DisplaySize;

                    LastGazeX = gazeInfo.X / size.X * 2 - 1;
                    LastGazeY = -(gazeInfo.Y / size.Y * 2 - 1); 
                }
            }
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                trackerClient?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
