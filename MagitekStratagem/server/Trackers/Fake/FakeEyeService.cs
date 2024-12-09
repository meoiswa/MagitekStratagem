using MagitekStratagemServer.Attributes;

namespace MagitekStratagemServer.Trackers.Fake
{
    [TrackerService("Fake Eye")]
    internal class FakeEyeService : BaseTrackerService
    {
        public FakeEyeService(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        public override void DoDispose()
        {
        }

        public override void DoStartTracking()
        {
            IsTracking = true;
        }

        public override void DoStopTracking()
        {
            IsTracking = false;
        }

        protected override void DoUpdate()
        {
            if (!IsTracking) return;

            LastGazeTimestamp = DateTime.Now.Ticks;
            double time = LastGazeTimestamp / 1e7; // Convert ticks to seconds
            LastGazeX = (float)Math.Cos(time);
            LastGazeY = (float)Math.Sin(time);
        }
    }
}
