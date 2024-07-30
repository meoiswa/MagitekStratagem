namespace MagitekStratagemPlugin.Eyeware
{
    public interface ITrackerListener
    {
        public abstract void OnTrackReady(TrackingEvent trackingEvent);
    }
}
