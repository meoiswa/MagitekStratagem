namespace MagitekStratagemServer.Trackers.Eyeware.Bindings.Client
{
    public struct GazeInfo
    {
        public Ray3D LeftEyeRay;
        public Ray3D RightEyeRay;
        public TrackingConfidence ConfidenceLeft;
        public TrackingConfidence ConfidenceRight;
        public bool IsLost;
    }
}
