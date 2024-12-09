namespace MagitekStratagemServer.Trackers.Eyeware.Bindings
{
    public interface ITrackerListener
    {
        public abstract void OnTrackReady(TrackingEvent trackingEvent);
    }
}
