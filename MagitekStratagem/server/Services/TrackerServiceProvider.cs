using MagitekStratagemServer.Trackers;
using MagitekStratagemServer.Trackers.Eyeware;
using MagitekStratagemServer.Trackers.Fake;
using MagitekStratagemServer.Trackers.OpentrackUdp;
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

        public ITrackerService? InitializeTracker(Type t)
        {
            if (t == null || !typeof(ITrackerService).IsAssignableFrom(t))
            {
                logger.LogError($"Invalid tracker type: {t?.FullName}");
                return null;
            }

            var tracker = Activator.CreateInstance(t, loggerFactory) as ITrackerService;
            if (tracker != null)
            {
                logger.LogTrace($"Initialized Tracker: {tracker.Name}");
            }
            else
            {
                logger.LogError($"Failed to create instance of tracker: {t.FullName}");
            }

            return tracker;
        }



        public void InitializeTrackers()
        {
            var trackerTypes = new List<Type>
            {
                typeof(TobiiService),
                typeof(BeamService),
                typeof(FakeEyeService),
                typeof(OpentrackUdpService)
            };

            foreach (var type in trackerTypes)
            {
                if (!trackerServices.ContainsKey(type.FullName!))
                {
                    ITrackerService? tracker = null;
                    try
                    {
                        tracker = InitializeTracker(type);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error initializing tracker: {type.FullName}");
                    }

                    if (tracker != null)
                    {
                        trackerServices[type.FullName!] = tracker;
                    }
                    else
                    {
                        logger.LogError($"Failed to initialize tracker: {type.FullName}");
                    }
                }
            }
        }

        public ITrackerService? GetTracker(string fullName)
        {
            logger.LogTrace($"Getting Tracker: {fullName}");
            var tracker = trackerServices.GetValueOrDefault(fullName);
            logger.LogTrace($"Tracker: {tracker?.Name ?? "null"}");
            return tracker;
        }

        public IEnumerable<Type> ListTrackers()
        {
            InitializeTrackers();
            logger.LogTrace("Listing Trackers");
            return trackerServices.Values.Select(t => t.GetType());
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
