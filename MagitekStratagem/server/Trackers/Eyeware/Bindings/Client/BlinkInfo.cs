namespace MagitekStratagemServer.Trackers.Eyeware.Bindings.Client
{
    public struct BlinkInfo
    {
        public bool LeftEyeClosed;
        public bool RightEyeClosed;
        public TrackingConfidence ConfidenceLeft;
        public TrackingConfidence ConfidenceRight;
        public bool IsLost;
    }
}
