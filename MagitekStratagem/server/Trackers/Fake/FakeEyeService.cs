using System.Numerics;
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

        protected override (bool, bool) DoUpdate()
        {
            if (!IsTracking) return (false, false);

            LastGazeTimestamp = DateTime.Now.Ticks;
            double time = LastGazeTimestamp / 1e7; // Convert ticks to seconds
            LastGazePoint = new Vector2((float)Math.Cos(time), (float)Math.Sin(time));

            LastHeadTimestamp = LastGazeTimestamp;
            LastHeadPosition = new Vector3((float)Math.Cos(-time), (float)Math.Sin(-time), (float)Math.Sin(-time));
            LastHeadRotation = new Vector3((float)Math.Sin(time) * 360, (float)Math.Cos(time) * 360, (float)Math.Sin(time) * 360);

            return (true, true);
        }
    }
}
