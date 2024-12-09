using MagitekStratagemServer.Attributes;
using MagitekStratagemServer.Trackers.Eyeware.Bindings;

namespace MagitekStratagemServer.Trackers.Eyeware
{
    [TrackerService("Eyeware Beam")]
    internal class BeamService : BaseTrackerService
    {
        public override bool IsTracking => trackerClient?.Connected() ?? false;

        private TrackerClient? trackerClient;

        public BeamService(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        public override void DoStartTracking()
        {
            if (trackerClient == null)
            {
                try
                {
                    trackerClient = new TrackerClient();
                    logger.LogTrace("Eyeware Beam Tracker Client Initialized");
                }
                catch (Exception e)
                {
                    logger.LogError(e.Message);
                }
            }
        }

        public override void DoStopTracking()
        {
            trackerClient?.Dispose();
            trackerClient = null;
        }

        protected override void DoUpdate()
        {
            if (trackerClient == null)
            {
                return;
            }

            if (trackerClient.Connected())
            {
                var gazeInfo = trackerClient.GetScreenGazeInfo();
                LastGazeTimestamp = DateTime.Now.Ticks;
                LastGazeX = gazeInfo.X;
                LastGazeY = gazeInfo.Y;
            }
        }

        public override void DoDispose()
        {
            trackerClient?.Dispose();
        }
    }
}
