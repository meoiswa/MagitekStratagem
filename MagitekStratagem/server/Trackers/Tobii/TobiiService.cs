using MagitekStratagemPlugin;
using MagitekStratagemServer.Attributes;
using MagitekStratagemServer.Trackers.Tobii.Bindings;

namespace MagitekStratagemServer.Trackers.Tobii
{
    [TrackerService("Tobii")]
    internal class TobiiService : BaseTrackerService
    {
        private readonly Api api;
        private readonly Device? device;

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

            if (urls.Count == 0)
            {
                logger.LogError("No Tobii devices found.");
                return;
            }

            device = api.CreateDevice(urls[0]);

            logger.LogTrace(device.ToString());
        }

        public override void DoStartTracking()
        {
            if (device == null)
            {
                IsTracking = false;
                logger.LogError("Tobii device is null. Cannot start tracking.");
                return;
            }

            IsTracking = true;
            device.Subscribe();
        }

        public override void DoStopTracking()
        {
            IsTracking = false;
            device?.Unsubscribe();
        }

        protected override (bool, bool) DoUpdate()
        {
            if (device == null || !IsTracking)
            {
                return (false, false);
            }

            var gazeUpdated = false;
            if (device.LastGazeTimestamp > LastGazeTimestamp)
            {
                LastGazeTimestamp = device.LastGazeTimestamp;
                LastGazePoint = device.LastGazePoint;
                gazeUpdated = true;
            }

            var headUpdated = false;
            if (device.LastHeadTimestamp > LastHeadTimestamp)
            {
                LastHeadTimestamp = device.LastHeadTimestamp;
                LastHeadPosition = device.LastHeadPosition;
                LastHeadRotation = device.LastHeadRotation;
                headUpdated = true;
            }

            return (gazeUpdated, headUpdated);
        }

        public override void DoDispose()
        {
            device?.Dispose();
            api?.Dispose();
        }
    }
}
