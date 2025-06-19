namespace MagitekStratagemServer.Trackers.Eyeware.Bindings.Client
{
    public struct HeadPoseInfo
    {
        public AffineTransform3D Transform;
        public bool IsLost;
        public ulong TrackSessionUid;
    }
}
