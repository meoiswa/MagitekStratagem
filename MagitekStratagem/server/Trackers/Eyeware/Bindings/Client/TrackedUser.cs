namespace MagitekStratagemServer.Trackers.Eyeware.Bindings.Client
{
    public struct TrackedUser
    {
        public uint Id;
        public TrackedPersonInfo TrackingInfo;

        public TrackedUser(uint id)
        {
            Id = id;
            TrackingInfo = new TrackedPersonInfo();
        }
    }
}
