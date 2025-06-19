namespace MagitekStratagemServer.Trackers.Eyeware.Bindings.Client
{
    public struct ScreenGazeInfo
    {
        public uint ScreenId;
        public uint X;
        public uint Y;
        public TrackingConfidence Confidence;
        public bool IsLost;
    }
}
