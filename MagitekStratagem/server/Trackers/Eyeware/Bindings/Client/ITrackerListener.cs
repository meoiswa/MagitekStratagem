namespace MagitekStratagemServer.Trackers.Eyeware.Bindings.Client
{
    public interface ITrackerListener
    {
        public abstract void OnTrackReady(TrackingEvent trackingEvent);
    }
}
