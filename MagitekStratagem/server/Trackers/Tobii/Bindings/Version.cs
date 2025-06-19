using System.Runtime.InteropServices;

namespace MagitekStratagemServer.Trackers.Tobii.Bindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TobiiVersion
    {
        public int major;
        public int minor;
        public int revision;
        public int build;
    }
}
