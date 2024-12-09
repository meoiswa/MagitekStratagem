using MagitekStratagemServer.Attributes;
using MagitekStratagemServer.Trackers.Tobii.Bindings;

namespace MagitekStratagemServer.Trackers.Tobii
{
    [TrackerService("Tobii")]
    internal class TobiiService : BaseTrackerService
    {
        private readonly Api api;
        private readonly Device device;

        public TobiiService(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            StreamEngine.SetLogger(loggerFactory.CreateLogger(nameof(StreamEngine)));

            var version = StreamEngine.GetApiVersion();
            logger.LogTrace($"Tobii Stream Engine API Version: {version.major}.{version.minor}.{version.revision}.{version.build}");

            api = StreamEngine.CreateApi();

            var urls = api.EnumerateDeviceUrls();

            foreach (var url in urls)
            {
                logger.LogTrace($"Tracker: {url}");
            }

            device = api.CreateDevice(urls[0]);

            logger.LogTrace(device.ToString());
        }

        public override void DoStartTracking()
        {
            IsTracking = true;
            device.Subscribe();
        }

        public override void DoStopTracking()
        {
            IsTracking = false;
            device.Unsubscribe();
        }

        protected override void DoUpdate()
        {
            if (device.GazeTimestamp > LastGazeTimestamp)
            {
                // TODO: Map coordinates using window rect
                LastGazeX = device.GazeX * 2 - 1;
                LastGazeY = -(device.GazeY * 2 - 1);
                LastGazeTimestamp = device.GazeTimestamp;
            }
        }

        public override void DoDispose()
        {
            device?.Dispose();
            api?.Dispose();
        }
    }
}
