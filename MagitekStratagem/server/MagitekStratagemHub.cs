using MagitekStratagemServer.Services;
using Microsoft.AspNetCore.SignalR;

namespace MagitekStratagemServer.Hubs
{
    public class MagitekStratagemHub : Hub
    {
        private readonly ILogger logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly ITrackerServiceProvider trackerServiceProvider;
        private readonly IHubContext<MagitekStratagemHub> hubContext;

        public static int ConnectedClients { get; private set; }

        public MagitekStratagemHub(
            ILoggerFactory loggerFactory,
            ITrackerServiceProvider trackerServiceProvider,
            IHubContext<MagitekStratagemHub> hubContext
        )
        {
            logger = loggerFactory.CreateLogger<MagitekStratagemHub>();
            this.loggerFactory = loggerFactory;
            this.trackerServiceProvider = trackerServiceProvider;
            this.hubContext = hubContext;
        }

        public override Task OnConnectedAsync()
        {
            ConnectedClients++;
            logger.LogInformation("Client connected, current clients: " + ConnectedClients);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            ConnectedClients--;
            logger.LogInformation("Client disconnected, current clients: " + ConnectedClients);
            return base.OnDisconnectedAsync(exception);
        }

        public async Task StartTracking(string fullName)
        {
            logger.LogDebug($"{Context.ConnectionId}: StartTracking: {fullName}");
            var trackerService = trackerServiceProvider.GetTracker(fullName);

            if (trackerService == null || trackerService.IsTracking)
            {
                return;
            }

            trackerService.StartTracking((tracker) =>
            {
                hubContext.Clients.All.SendAsync("TrackerUpdate", tracker.GetType().FullName, tracker.LastGazeTimestamp, tracker.LastGazeX, tracker.LastGazeY).Wait();
            });

            if (trackerService.IsTracking)
            {
                await Clients.All.SendAsync("TrackingStarted", fullName);
            }
        }

        public async Task StopTracking(string fullName)
        {
            logger.LogDebug($"{Context.ConnectionId}: StopTracking: {fullName}");
            var trackerService = trackerServiceProvider.GetTracker(fullName);
            if (trackerService == null || !trackerService.IsTracking)
            {
                return;
            }

            trackerService.StopTracking();

            if (!trackerService.IsTracking)
            {
                await Clients.All.SendAsync("TrackingStopped", fullName);
            }
        }

        public async Task GetTrackerServices()
        {
            logger.LogDebug($"{Context.ConnectionId}: Getting Tracker Services");
            var implementations = trackerServiceProvider.ListTrackers()
                .Select(impl => new TrackerServiceDto
                {
                    FullName = impl.FullName,
                    Name = impl.Name
                }).ToArray();
            await Clients.Caller.SendAsync("TrackerServices", implementations);
        }
    }
}
