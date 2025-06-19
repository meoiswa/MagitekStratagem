using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using MagitekStratagemServer.Attributes;

namespace MagitekStratagemServer.Trackers;

internal abstract class BaseTrackerService : ITrackerService
{
    private Thread? updateThread;
    private CancellationTokenSource? cancellationToken;
    protected readonly ILoggerFactory loggerFactory;
    protected readonly ILogger logger;

    public string Name => GetType().GetCustomAttribute<TrackerServiceAttribute>()?.Name ?? GetType().Name;

    public virtual bool IsTracking { get; protected set; } = false;

    public long LastGazeTimestamp { get; protected set; } = 0;
    public Vector2 LastGazePoint { get; protected set; } = Vector2.Zero;

    public long LastHeadTimestamp { get; protected set; } = 0;
    public Vector3 LastHeadPosition { get; protected set; } = Vector3.Zero;
    public Vector3 LastHeadRotation { get; protected set; } = Vector3.Zero;

    private int rate = 1000 / 120;

    public BaseTrackerService(ILoggerFactory loggerFactory)
    {
        this.loggerFactory = loggerFactory;
        logger = loggerFactory.CreateLogger(Name);
        logger.LogDebug($"{Name} Initialized");
    }

    protected void Start(Action<ITrackerService> gazeCallback, Action<ITrackerService> headCallback)
    {
        if (updateThread != null)
        {
            return;
        }

        cancellationToken = new CancellationTokenSource();
        updateThread = new Thread(() =>
        {
            logger.LogTrace("Update Thread Started");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (!cancellationToken.Token.IsCancellationRequested)
            {
                stopwatch.Restart();
                var (gazeUpdated, headUpdated) = DoUpdate();
                if (gazeUpdated)
                {
                    gazeCallback(this);
                }
                if (headUpdated)
                {
                    headCallback(this);
                }
                var sleepTime = rate - (stopwatch.ElapsedTicks / 1000);
                if (sleepTime > 0)
                {
                    Thread.Sleep((int)sleepTime);
                }
            }
            logger.LogTrace("Update Thread Finished");
        });
        updateThread.Start();
    }

    protected void Stop()
    {
        if (updateThread != null)
        {
            cancellationToken!.Cancel();
            updateThread.Join();
            updateThread = null;
        }
    }

    protected abstract (bool, bool) DoUpdate();

    public abstract void DoStartTracking();

    public abstract void DoStopTracking();

    public abstract void DoDispose();

    public void Dispose()
    {
        logger.LogTrace($"Disposing {Name} Service");
        Stop();
        DoDispose();
    }

    public void StartTracking(Action<ITrackerService> gazeCallback, Action<ITrackerService> headCallback)
    {
        logger.LogTrace($"Starting {Name} Tracking");
        DoStartTracking();
        Start(gazeCallback, headCallback);
    }

    public void StopTracking()
    {
        logger.LogTrace($"Stopping {Name} Tracking");
        Stop();
        DoStopTracking();
    }
}
