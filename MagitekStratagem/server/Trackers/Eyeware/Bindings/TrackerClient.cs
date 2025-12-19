using Dalamud.Bindings.ImGui;
using Eyeware.BeamEyeTracker;

namespace MagitekStratagemServer.Trackers.Eyeware.Bindings
{
    public class TrackerClient : IDisposable
    {
        public const string Library = "beam_eye_tracker_client";

        private API? beamAPI;

        public TrackerClient(string friendlyName = "MagitekStratagem")
        {
            var viewport = ImGui.GetMainViewport();
            var p00 = new Point((int)viewport.Pos.X, (int)viewport.Pos.Y);
            var p11 = new Point((int)(viewport.Pos.X + viewport.Size.X), (int)(viewport.Pos.Y + viewport.Size.Y));
            var initialViewportGeometry = new ViewportGeometry(p00, p11);
            beamAPI = new API(friendlyName, initialViewportGeometry);

            var listener = new TrackingListener();
            beamAPI.StartReceivingTrackingDataOnListener(listener);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (beamAPI != null)
            {
                beamAPI.Dispose();
                beamAPI = null;
            }
        }

        public bool Connected()
        {
            if (beamAPI == null)
            {
                return false;
            }

            switch (beamAPI.GetTrackingDataReceptionStatus())
            {
                case TrackingDataReceptionStatus.ReceivingTrackingData:
                    return true;
                case TrackingDataReceptionStatus.AttemptingTrackingAutoStart:
                case TrackingDataReceptionStatus.NotReceivingTrackingData:
                default:
                    return false;
            }
        }

        public TrackingStateSet? GetTrackingStateSet()
        {
            if (beamAPI == null)
            {
                return null;
            }

            return beamAPI.GetLatestTrackingStateSet();
        }

        ~TrackerClient()
        {
            Dispose(false);
        }
    }
}
