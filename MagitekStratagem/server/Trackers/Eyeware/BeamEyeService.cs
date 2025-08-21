using System.Numerics;
using Dalamud.Bindings.ImGui;
using Eyeware.BeamEyeTracker;
using MagitekStratagemServer.Attributes;
using MagitekStratagemServer.Trackers.Eyeware.Bindings;

namespace MagitekStratagemServer.Trackers.Eyeware
{
    [TrackerService("Eyeware Beam")]
    internal class BeamService : BaseTrackerService
    {
        public override bool IsTracking => trackerClient?.Connected() ?? false;

        private TrackerClient? trackerClient;

        public BeamService(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            trackerClient = new TrackerClient();
            logger.LogTrace("Eyeware Beam Tracker Client Initialized");
        }

        public override void DoStartTracking()
        {
            // Do nothing;
        }

        public override void DoStopTracking()
        {
            // Do nothing;
        }

        protected override (bool, bool) DoUpdate()
        {
            if (trackerClient == null || !trackerClient.Connected())
            {
                return (false, false);
            }

            var state = trackerClient.GetTrackingStateSet();

            if (state == null)
            {
                return (false, false);
            }

            var gazeInfo = state.UserState.UnifiedScreenGaze;

            var gazeUpdated = false;
            if (gazeInfo.Confidence != TrackingConfidence.LostTracking)
            {
                LastGazeTimestamp = DateTime.Now.Ticks;
                LastGazePoint = new Vector2(gazeInfo.PointOfRegard.X, gazeInfo.PointOfRegard.Y);
                gazeUpdated = true;
            }

            var headUpdated = false;
            var headInfo = state.UserState.HeadPose;
            if (headInfo.Confidence != TrackingConfidence.LostTracking)
            {
                var translation = headInfo.TranslationFromHcsToWcs;
                var rotation = headInfo.RotationFromHcsToWcs;
                LastHeadTimestamp = DateTime.Now.Ticks;
                LastHeadPosition = new Vector3(translation.X, translation.Y, translation.Z);
                LastHeadRotation = new Vector3(rotation[0], rotation[1], rotation[2]);
                headUpdated = true;
            }

            return (gazeUpdated, headUpdated);
        }

        public override void DoDispose()
        {
            trackerClient?.Dispose();
            trackerClient = null;
        }

        public Vector3D ToEulerAngles(float[] rotation)
        {
            // rotation: float[9], row-major order (m00, m01, m02, m10, m11, m12, m20, m21, m22)
            // Map rotation matrix to Euler angles (Pitch, Yaw, Roll)
            float m00 = rotation[0], m01 = rotation[1], m02 = rotation[2];
            float m10 = rotation[3], m11 = rotation[4], m12 = rotation[5];
            float m20 = rotation[6], m21 = rotation[7], m22 = rotation[8];

            float sy = (float)Math.Sqrt(m21 * m21 + m22 * m22);
            bool singular = sy < 1e-6;
            float x, y, z;
            if (!singular)
            {
                x = (float)Math.Atan2(m21, m22); // Pitch
                y = (float)Math.Atan2(-m20, sy); // Yaw
                z = (float)Math.Atan2(m10, m00); // Roll
            }
            else
            {
                x = (float)Math.Atan2(-m12, m11);
                y = (float)Math.Atan2(-m20, sy);
                z = 0;
            }

            return new Vector3D
            {
                X = x,
                Y = y,
                Z = z
            };
        }
    }
}
