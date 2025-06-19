using System.Runtime.InteropServices;

namespace MagitekStratagemServer.Trackers.Tobii.Bindings
{
    [StructLayout(LayoutKind.Explicit)]
    public struct NotificationValueUnion
    {
        [FieldOffset(0)] public float float_;
        [FieldOffset(0)] public int state;
        // [FieldOffset(0)] public DisplayArea display_area; // Placeholder, define as needed
        [FieldOffset(0)] public uint uint_;
        [FieldOffset(0)] public int enabled_eye;
        // [FieldOffset(0)] public IntPtr string_; // For string pointer
    }
}
