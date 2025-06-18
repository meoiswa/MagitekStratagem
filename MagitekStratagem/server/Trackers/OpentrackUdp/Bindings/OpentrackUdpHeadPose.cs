using System;
using System.Runtime.InteropServices;

namespace MagitekStratagemServer.Trackers.OpentrackUdp.Bindings
{
    /// <summary>
    /// Represents a 6DOF head pose packet from Opentrack UDP (Yaw, Pitch, Roll, X, Y, Z)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct OpentrackUdpHeadPose
    {
        public double X;     // Position (left/right, centimeters)
        public double Y;     // Position (up/down, centimeters)
        public double Z;     // Position (forward/back, centimeters)
        public double Yaw;   // Rotation (left/right, degrees)
        public double Pitch; // Rotation (up/down, degrees)
        public double Roll;  // Rotation (tilt, degrees)
    }
}
