using MagitekStratagemServer.Trackers;
using MagitekStratagemServer.Trackers.Eyeware;
using MagitekStratagemServer.Trackers.Fake;
using MagitekStratagemServer.Trackers.Tobii;

namespace MagitekStratagemServer.Services
{
    public class TrackerServiceProvider : ITrackerServiceProvider, IDisposable
    {
        private Dictionary<string, ITrackerService> trackerServices = new();
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger logger;

        public TrackerServiceProvider(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger<TrackerServiceProvider>();

            logger.LogTrace("Tracker Service Provider Initialized");
        }

        public ITrackerService? GetTracker(string fullName)
        {
            logger.LogTrace($"Getting Tracker: {fullName}");
            var tracker = trackerServices.GetValueOrDefault(fullName);

            if (tracker == null)
            {
                if (fullName == typeof(TobiiService).FullName)
                {
                    tracker = new TobiiService(loggerFactory);
                }
                else if (fullName == typeof(BeamService).FullName)
                {
                    tracker = new BeamService(loggerFactory);
                }
                else if (fullName == typeof(FakeEyeService).FullName)
                {
                    tracker = new FakeEyeService(loggerFactory);
                }

                if (tracker != null)
                {
                    trackerServices[fullName] = tracker;
                }
            }

            logger.LogTrace($"Tracker: {tracker?.Name ?? "null"}");

            return tracker;
        }

        public IEnumerable<Type> ListTrackers()
        {
            logger.LogTrace("Listing Trackers");
            var types = new List<Type>()
            {
                typeof(FakeEyeService),
                typeof(TobiiService),
                typeof(BeamService),
            };
            return types;
        }

        public void Dispose()
        {
            foreach (var tracker in trackerServices.Values)
            {
                tracker.Dispose();
            }
            trackerServices.Clear();
        }
    }
}
