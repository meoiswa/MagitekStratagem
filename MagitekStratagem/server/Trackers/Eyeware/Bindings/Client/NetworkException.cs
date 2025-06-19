namespace MagitekStratagemServer.Trackers.Eyeware.Bindings.Client
{
    public class NetworkException : Exception
    {
        public NetworkException(string errorMsg) : base(errorMsg) { }
    }
}
