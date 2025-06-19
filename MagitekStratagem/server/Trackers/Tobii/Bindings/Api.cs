namespace MagitekStratagemServer.Trackers.Tobii.Bindings
{
    public class Api : UnmanagedObject
    {
        protected override void Destroy()
        {
            StreamEngine.DestroyApi(Ptr);
        }

        public Api(nint ptr) : base(ptr)
        {
        }

        public List<string> EnumerateDeviceUrls()
        {
            return StreamEngine.EnumerateLocalDevices(Ptr);
        }

        public Device CreateDevice(string url)
        {
            return StreamEngine.CreateDevice(Ptr, url);
        }

        public void WaitForCallbacks(IEnumerable<Device> devices)
        {
            StreamEngine.WaitForCallbacks(devices.Select((device) => device.Ptr));
        }
    }
}
