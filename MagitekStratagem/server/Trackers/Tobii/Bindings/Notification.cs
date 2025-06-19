using System.Runtime.InteropServices;

namespace MagitekStratagemServer.Trackers.Tobii.Bindings
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Notification
    {
        public NotificationType type;
        public NotificationValueType value_type;
        public NotificationValueUnion value;
    }
}
