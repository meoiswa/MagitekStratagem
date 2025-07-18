using System.Numerics;
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
            trackerClient = new TrackerClient((error) =>
            {
                logger.LogError($"Eyeware Beam Tracker Error: {Enum.GetName(error)}");
            });
            logger.LogTrace("Eyeware Beam Tracker Client Initialized");
        }

        public override void DoStartTracking()
        {
            // Do nothing;
        }

        public override void DoStopTracking()
        {
            // Do nothing;
        }

        protected override (bool, bool) DoUpdate()
        {
            if (trackerClient == null || !trackerClient.Connected())
            {
                return (false, false);
            }

            var gazeInfo = trackerClient.GetScreenGazeInfo();

            var gazeUpdated = false;
            if (!gazeInfo.IsLost)
            {
                LastGazeTimestamp = DateTime.Now.Ticks;
                LastGazePoint = new Vector2(gazeInfo.X, gazeInfo.Y);
                gazeUpdated = true;
            }

            var headUpdated = false;
            var headInfo = trackerClient.GetHeadPoseInfo();
            if (!headInfo.IsLost)
            {
                LastHeadTimestamp = DateTime.Now.Ticks;
                LastHeadPosition = headInfo.Transform.Translation;
                LastHeadRotation = headInfo.Transform.ToEulerAngles();
                headUpdated = true;
            }

            return (gazeUpdated, headUpdated);
        }

        public override void DoDispose()
        {
            trackerClient?.Dispose();
            trackerClient = null;
        }
    }
}
