/**
 * Copyright (C) 2025 Eyeware Tech SA
 *
 * All rights reserved
 */
using System.Runtime.InteropServices;

namespace Eyeware.BeamEyeTracker
{
    /// <summary>
    /// Global constants for the Beam Eye Tracker SDK.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Special value indicating an invalid timestamp.
        ///
        /// @rst
        /// .. note:: See :ref:`about_timestamps` for more information.
        /// @endrst
        /// </summary>
        public const double NullDataTimestamp = -1.0;
    }

    #region Enums and Structs
    /// <summary>
    /// Represents the status of the tracking data reception.
    /// This state indicates whether the client is receiving frame-by-frame tracking data or not,
    /// regardless of whether the user is being tracked or not.
    ///
    /// @rst
    /// .. note:: See :ref:`about_tracking_data_reception_status` for more information.
    /// @endrst
    /// </summary>
    public enum TrackingDataReceptionStatus : Int32
    {
        /// <summary>
        /// The client is not currently receiving data from the Beam Eye Tracker.
        /// There could be multiple reasons why this is the case but in general it means that the user
        /// should manually start the Beam Eye Tracker application (if not yet launched), sign in, and/or
        /// successfully activate "Gaming Extensions" (as of Beam Eye Tracker v2.0).
        /// </summary>
        /// .. note::
        ///     In general, this is when manual user intervention is needed to configure the tracker.
        ///     Alternatively, see :ref:`about_auto_start_tracking`.
        NotReceivingTrackingData = 0,

        /// <summary>
        /// It is actively connected to the Beam Eye Tracker and regularly receiving tracking data.
        /// Please note this does not imply that the user is being successfully tracked.
        /// It merely indicates that the Beam Eye Tracker is active and sending updates,
        /// even if the user is not being tracked.
        /// </summary>
        ReceivingTrackingData = 1,

        /// <summary>
        /// It is trying to launch the Beam Eye Tracker and/or start its tracking after an explicit
        /// request to auto-start tracking.
        /// While in this state, there are multiple things that could be happening behind the scenes:
        /// - Checking if the Beam Eye Tracker is installed.
        /// - Checking if the Beam Eye Tracker is running.
        /// - Launching the Beam Eye Tracker.
        /// - Requesting the Beam Eye Tracker to activate Gaming Extensions (or other API enabling mode)
        /// Thus, depending on the state of the Beam Eye Tracker, this could fail, succeed quickly (~100ms)
        /// or succeed taking a while (~10 seconds).
        /// </summary>
        AttemptingTrackingAutoStart = 2
    }

    /// <summary>
    /// SDK version information
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct Version
    {
        /// <summary>Major version number</summary>
        public UInt32 Major;

        /// <summary>Minor version number</summary>
        public UInt32 Minor;

        /// <summary>Patch level</summary>
        public UInt32 Patch;

        /// <summary>Internal padding for alignment</summary>
        public UInt32 Padding;
    }

    /// <summary>
    /// 2D integer point coordinates.
    /// Used primarily for screen coordinates in the unified coordinate system.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct Point
    {
        /// <summary>X coordinate</summary>
        public Int32 X;

        /// <summary>Y coordinate</summary>
        public Int32 Y;

        /// <summary>
        /// Initializes a new instance of the Point structure.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public Point(Int32 x, Int32 y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// 2D floating point coordinates.
    /// Used for normalized viewport coordinates and precise positioning.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct PointF
    {
        /// <summary>X coordinate</summary>
        public float X;

        /// <summary>Y coordinate</summary>
        public float Y;

        /// <summary>
        /// Initializes a new instance of the PointF structure.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public PointF(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Viewport geometry definition.
    /// It is used to map from unified screen coordinates to the viewport normalized coordinates, ranging
    /// [0.0, 1.0] for a point inside the viewport rectangle.
    /// </summary>
    /// @rst
    /// .. note::
    ///     See :ref:`viewport` for more information.
    /// @endrst
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct ViewportGeometry
    {
        /// <summary>
        /// Point, in unified screen coordinates, where the (0.0, 0.0) point of the viewport is.
        /// The point is inclusive, i.e., considered part of the border of the viewport rectangle.
        /// </summary>
        public Point Point00;

        /// <summary>
        /// Point, in unified screen coordinates, where the (1.0, 1.0) point of the viewport is.
        /// The point is inclusive, i.e., considered part of the border of the viewport rectangle.
        /// </summary>
        public Point Point11;

        /// <summary>
        /// Initializes a new instance of the ViewportGeometry structure.
        /// </summary>
        /// <param name="point00">Point in unified screen coordinates where the (0.0, 0.0) point of the viewport is</param>
        /// <param name="point11">Point in unified screen coordinates where the (1.0, 1.0) point of the viewport is</param>
        public ViewportGeometry(Point point00, Point point11)
        {
            Point00 = point00;
            Point11 = point11;
        }
    }

    /// <summary>
    /// Representation of a 3D vector or 3D point.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct Vector3D
    {
        /// <summary>X coordinate</summary>
        public float X;

        /// <summary>Y coordinate</summary>
        public float Y;

        /// <summary>Z coordinate</summary>
        public float Z;

        /// <summary>Internal padding for alignment</summary>
        UInt32 _Padding;
    }

    /// <summary>
    /// Realibility measure for obtained tracking results.
    /// </summary>
    /// @rst
    /// .. note::
    ///     See :ref:`tracking_confidence` for a detailed explanation.
    /// @endrst
    public enum TrackingConfidence : Int32
    {
        /// <summary>The signal/data in question is unavailable and should be discarded.</summary>
        LostTracking = 0,

        /// <summary>Tracking is present but highly uncertain.</summary>
        Low = 1,

        /// <summary>Tracking reliability is fair.</summary>
        Medium = 2,

        /// <summary>Tracking is as reliable as it gets.</summary>
        High = 3
    }

    /// <summary>
    /// Represents gaze information in screen coordinates.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct UnifiedScreenGaze
    {
        /// <summary>
        /// The confidence of the tracking result.
        /// </summary>
        public TrackingConfidence Confidence;
        UInt32 _Padding;

        /// <summary>
        /// Point where the user is looking at, kept within bounds of the screen(s) resolution(s).
        /// </summary>
        public Point PointOfRegard;

        /// <summary>
        /// Point where the user is looking at, which may be outside the physical screen space.
        /// This alternative to PointOfRegard is important because:
        /// - having a continuous signal crossing the screen boundaries is useful for smoother
        ///   animations, or controlling elements that are not confined to the screen
        /// - it allows you to implement heuristics to account for eye tracker inaccuracies
        ///   nearby the screen bounds.
        /// </summary>
        public Point UnboundedPointOfRegard;
    }

    /// <summary>
    /// Represents gaze information in viewport coordinates.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct ViewportGaze
    {
        /// <summary>
        /// The confidence of the tracking result.
        /// </summary>
        public TrackingConfidence Confidence;
        UInt32 _Padding;

        /// <summary>
        /// Point where the user is looking at, normalized such that, if the gaze is inside the viewport,
        /// then the values are in the range [0, 1]. However, it can be negative or exceed 1, if the
        /// gaze is outside the viewport.
        /// </summary>
        public PointF NormalizedPointOfRegard;
    }

    /// <summary>
    /// Represents information of the head pose for the given time instant.
    /// </summary>
    /// @rst
    /// .. note::
    ///     See :ref:`head_pose` for a detailed explanation.
    /// @endrst
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct HeadPose
    {
        /// <summary>
        /// The confidence of the tracking result.
        /// </summary>
        public TrackingConfidence Confidence;

        /// <summary>
        /// Rotation matrix, with respect to the World Coordinate System (WCS).
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public float[] RotationFromHcsToWcs; // 3x3 matrix stored as 1D array

        /// <summary>
        /// Translation vector, with respect to the World Coordinate System (WCS).
        /// </summary>
        public Vector3D TranslationFromHcsToWcs;

        /// <summary>
        /// Indicates the ID of the session of uninterrupted consecutive tracking.
        /// When a user is being tracked over consecutive frames, the track_session_uid
        /// is kept unchanged. However, if the user goes out of frame or turns around such
        /// that they can no longer be tracked, then this number is incremented once the
        /// user is detected again.
        /// </summary>
        public ulong TrackSessionUid;
    }

    /// <summary>
    /// Complete user tracking state.
    /// </summary>
    /// @rst
    /// .. note::
    ///     See :ref:`about_real_time_tracking` for a detailed explanation.
    /// @endrst
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct UserState
    {
        /// <summary>Structure version for compatibility</summary>
        public UInt64 StructVersion;

        /// <summary>Data timestamp</summary>
        public double TimestampInSeconds;

        /// <summary>3D head position and orientation</summary>
        public HeadPose HeadPose;

        /// <summary>Gaze in screen coordinates</summary>
        public UnifiedScreenGaze UnifiedScreenGaze;

        /// <summary>Normalized viewport gaze</summary>
        public ViewportGaze ViewportGaze;

        /// <summary>Reserved for future use</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] Reserved;

        /// <summary>
        /// Creates a new instance of UserState with default values.
        /// </summary>
        /// <returns>A new UserState instance with default values.</returns>
        public static UserState Create()
        {
            return new UserState
            {
                StructVersion = 1,
                TimestampInSeconds = Constants.NullDataTimestamp,
                HeadPose = new HeadPose(),
                UnifiedScreenGaze = new UnifiedScreenGaze(),
                ViewportGaze = new ViewportGaze(),
                Reserved = new byte[128]
            };
        }
    }

    /// <summary>
    /// Represents the 3D transform parameters to be applied to the in-game camera.
    /// </summary>
    /// @rst
    /// .. warning:: Please see :ref:`sim_mapping` for explanation on how to interpret the parameters.
    /// @endrst
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct SimCameraTransform3D
    {
        /// <summary>Roll angle in radians.</summary>
        public float RollInRadians;

        /// <summary>Pitch angle in radians.</summary>
        public float PitchInRadians;

        /// <summary>Yaw angle in radians.</summary>
        public float YawInRadians;

        /// <summary>X translation in meters.</summary>
        public float XInMeters;

        /// <summary>Y translation in meters.</summary>
        public float YInMeters;

        /// <summary>Z translation in meters.</summary>
        public float ZInMeters;
    }

    /// <summary>
    /// Holds the required data to achieve real-time immersive controls of the in-game camera.
    /// To consume the parameters, we do not recommend accessing the EyeTrackingPoseComponent
    /// and HeadTrackingPoseComponent directly, but instead, use the provided method that
    /// applies a weighted combination of the two components.
    /// </summary>
    /// @rst
    /// .. note::
    ///     See :ref:`about_sim_game_camera_state` for a detailed explanation.
    /// @endrst
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct SimGameCameraState
    {
        /// <summary>Structure version for compatibility</summary>
        public UInt64 StructVersion;

        /// <summary>
        /// The timestamp of this update, in seconds. If it is equal to
        /// NullDataTimestamp, then the rest of the data is invalid and should be ignored.
        /// This is effectively a counter since the tracking started. Note that given that the user can
        /// turn off/on the tracking at will, this counter can't be assumed to be strictly monotonic.
        /// </summary>
        public double TimestampInSeconds;

        /// <summary>
        /// The camera transform if based solely on the eye tracking data.
        /// See SimCameraTransform3D for interpretation of the parameters.
        /// We do not recommend using this signal directly and instead use the ComputeSimGameCameraTransformParameters
        /// method to get the final camera transform.
        /// </summary>
        public SimCameraTransform3D EyeTrackingPoseComponent;

        /// <summary>
        /// The camera transform if based solely on the head tracking data.
        /// See SimCameraTransform3D for interpretation of the parameters.
        /// We do not recommend using this signal directly and instead use the ComputeSimGameCameraTransformParameters
        /// method to get the final camera transform.
        /// </summary>
        public SimCameraTransform3D HeadTrackingPoseComponent;

        /// <summary>Reserved for future use</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public ulong[] Reserved;

        /// <summary>
        /// Creates a new instance of SimGameCameraState with default values.
        /// </summary>
        /// <returns>A new SimGameCameraState instance with default values.</returns>
        public static SimGameCameraState Create()
        {
            return new SimGameCameraState
            {
                StructVersion = 1,
                TimestampInSeconds = Constants.NullDataTimestamp,
                EyeTrackingPoseComponent = new SimCameraTransform3D(),
                HeadTrackingPoseComponent = new SimCameraTransform3D(),
                Reserved = new ulong[128]
            };
        }
    }

    /// <summary>
    /// Represents the information you need to implement an immersive HUD in your game.
    /// In many games, the HUD is implemented by UI elements on the 4 corners of the screen,
    /// but this struct provides values for all non-center 8 regions of the screen (corners and mid-edges).
    /// The values are in the range [0, 1], where 0 means the user is not looking at the element, and 1
    /// means the user is looking at the element. In most cases, you can simply map to a boolean value
    /// using 0.5 as threshold.
    /// </summary>
    /// @rst
    /// .. note::
    ///     See :ref:`about_game_immersive_hud_ready_to_use_signals` for more information.
    /// @endrst
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct GameImmersiveHUDState
    {
        /// <summary>Structure version for compatibility</summary>
        public UInt64 StructVersion;

        /// <summary>
        /// The timestamp of this update, in seconds. If it is equal to
        /// NullDataTimestamp, then the rest of the data is invalid and should be ignored.
        /// This is effectively a counter since the tracking started. Note that given that the user can
        /// turn off/on the tracking at will, this counter can't be assumed to be strictly monotonic.
        /// </summary>
        public double TimestampInSeconds;

        /// <summary>Signal of whether the user is looking at the top-left region of the screen.</summary>
        public float LookingAtViewportTopLeft;

        /// <summary>Signal of whether the user is looking at the top-middle region of the screen.</summary>
        public float LookingAtViewportTopMiddle;

        /// <summary>Signal of whether the user is looking at the top-right region of the screen.</summary>
        public float LookingAtViewportTopRight;

        /// <summary>Signal of whether the user is looking at the center-left region of the screen.</summary>
        public float LookingAtViewportCenterLeft;

        /// <summary>Signal of whether the user is looking at the center-right region of the screen.</summary>
        public float LookingAtViewportCenterRight;

        /// <summary>Signal of whether the user is looking at the bottom-left region of the screen.</summary>
        public float LookingAtViewportBottomLeft;

        /// <summary>Signal of whether the user is looking at the bottom-middle region of the screen.</summary>
        public float LookingAtViewportBottomMiddle;

        /// <summary>Signal of whether the user is looking at the bottom-right region of the screen.</summary>
        public float LookingAtViewportBottomRight;

        /// <summary>Reserved for future use</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] Reserved;

        /// <summary>
        /// Creates a new instance of GameImmersiveHUDState with default values.
        /// </summary>
        /// <returns>A new GameImmersiveHUDState instance with default values.</returns>
        public static GameImmersiveHUDState Create()
        {
            return new GameImmersiveHUDState
            {
                StructVersion = 1,
                TimestampInSeconds = Constants.NullDataTimestamp,
                LookingAtViewportTopLeft = 0,
                LookingAtViewportTopMiddle = 0,
                LookingAtViewportTopRight = 0,
                LookingAtViewportCenterLeft = 0,
                LookingAtViewportCenterRight = 0,
                LookingAtViewportBottomLeft = 0,
                LookingAtViewportBottomMiddle = 0,
                LookingAtViewportBottomRight = 0,
                Reserved = new byte[128]
            };
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct FoveationRadii
    {
        /// <summary>Inner area should be rendered at highest definition.</summary>
        public float RadiusLevel1;

        /// <summary>Second level of definition.</summary>
        public float RadiusLevel2;

        /// <summary>Third level of definition.</summary>
        public float RadiusLevel3;

        /// <summary>Outer area should be rendered at lowest definition.</summary>
        public float RadiusLevel4;

        /// <summary>
        /// Initializes a new instance of the FoveationRadii structure.
        /// </summary>
        /// <param name="radiusLevel1">Inner area should be rendered at highest definition.</param>
        /// <param name="radiusLevel2">Second level of definition.</param>
        /// <param name="radiusLevel3">Third level of definition.</param>
        /// <param name="radiusLevel4">Outer area should be rendered at lowest definition.</param>
        public FoveationRadii(
            float radiusLevel1,
            float radiusLevel2,
            float radiusLevel3,
            float radiusLevel4
        )
        {
            RadiusLevel1 = radiusLevel1;
            RadiusLevel2 = radiusLevel2;
            RadiusLevel3 = radiusLevel3;
            RadiusLevel4 = radiusLevel4;
        }
    }

    /// <summary>
    /// Represents the information needed to implement foveated rendering.
    /// </summary>
    /// @rst
    /// .. note:: See :ref:`about_foveated_rendering` for more information.
    /// @endrst
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct FoveatedRenderingState
    {
        /// <summary>Structure version for compatibility</summary>
        public UInt64 StructVersion;

        /// <summary>
        /// The timestamp of this update, in seconds. If it is equal to
        /// NullDataTimestamp, then the rest of the data is invalid and should be ignored.
        /// This is effectively a counter since the tracking started. Note that given that the user can
        /// turn off/on the tracking at will, this counter can't be assumed to be strictly monotonic.
        /// </summary>
        public double TimestampInSeconds;

        /// <summary>The center of the foveated rendering region normalized by the viewport width and height.</summary>
        public PointF NormalizedFoveationCenter;

        /// <summary>The radii of the foveated rendering regions normalized by the viewport width.</summary>
        public FoveationRadii NormalizedFoveationRadii;

        /// <summary>Reserved for future use</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] Reserved;

        /// <summary>
        /// Creates a new instance of FoveatedRenderingState with default values.
        /// </summary>
        /// <returns>A new FoveatedRenderingState instance with default values.</returns>
        public static FoveatedRenderingState Create()
        {
            return new FoveatedRenderingState
            {
                StructVersion = 1,
                TimestampInSeconds = Constants.NullDataTimestamp,
                NormalizedFoveationCenter = new PointF(0, 0),
                NormalizedFoveationRadii = new FoveationRadii(0, 0, 0, 0),
                Reserved = new byte[128]
            };
        }
    }
    #endregion

    /// <summary>
    /// Internal delegate for receiving tracking data reception status updates.
    /// </summary>
    /// <param name="status">The current tracking data reception status.</param>
    /// <param name="userData">User-provided data pointer.</param>
    internal delegate void TrackingDataReceptionStatusCallback(
        TrackingDataReceptionStatus status,
        IntPtr userData
    );

    /// <summary>
    /// Internal delegate for receiving tracking data updates.
    /// </summary>
    /// <param name="trackingStateSetHandle">Handle to the tracking state set.</param>
    /// <param name="timestamp">Timestamp of the tracking data.</param>
    /// <param name="userData">User-provided data pointer.</param>
    internal delegate void TrackingDataCallback(
        IntPtr trackingStateSetHandle,
        double timestamp,
        IntPtr userData
    );

    /// <summary>
    /// Contains the complete set of tracking data for a single frame, including user state,
    /// game camera state, and immersive HUD state.
    /// </summary>
    /// <remarks>
    /// This class provides access to all tracking data components through properties.
    /// The data is read immediately upon construction and remains immutable thereafter.
    /// </remarks>
    public class TrackingStateSet
    {
        private readonly UserState _userState;
        private readonly SimGameCameraState _simGameCameraState;
        private readonly GameImmersiveHUDState _gameImmersiveHUDState;
        private readonly FoveatedRenderingState _foveatedRenderingState;

        /// <summary>
        /// Initializes a new instance of the TrackingStateSet class.
        /// </summary>
        /// <param name="trackingStateSetHandle">Handle to the native tracking state set.</param>
        /// <exception cref="ArgumentException">Thrown when the handle is invalid (zero).</exception>
        internal TrackingStateSet(IntPtr trackingStateSetHandle)
        {
            if (trackingStateSetHandle == IntPtr.Zero)
                throw new ArgumentException("Invalid handle", nameof(trackingStateSetHandle));

            // Read all states immediately from the handle
            IntPtr userStatePtr = EW_BET_API_GetUserState(trackingStateSetHandle);
            IntPtr cameraStatePtr = EW_BET_API_GetSimGameCameraState(trackingStateSetHandle);
            IntPtr hudStatePtr = EW_BET_API_GetGameImmersiveHUDState(trackingStateSetHandle);
            IntPtr foveatedRenderingStatePtr = EW_BET_API_GetFoveatedRenderingState(
                trackingStateSetHandle
            );

            _userState = Marshal.PtrToStructure<UserState>(userStatePtr);
            _simGameCameraState = Marshal.PtrToStructure<SimGameCameraState>(cameraStatePtr);
            _gameImmersiveHUDState = Marshal.PtrToStructure<GameImmersiveHUDState>(hudStatePtr);
            _foveatedRenderingState = Marshal.PtrToStructure<FoveatedRenderingState>(
                foveatedRenderingStatePtr
            );
        }

        /// <summary>
        /// Gets the current user state, including head pose and gaze information.
        /// </summary>
        public UserState UserState => _userState;

        /// <summary>
        /// Gets the current game camera state, including eye and head tracking components.
        /// </summary>
        public SimGameCameraState SimGameCameraState => _simGameCameraState;

        /// <summary>
        /// Gets the current immersive HUD state, including viewport region indicators.
        /// </summary>
        public GameImmersiveHUDState GameImmersiveHUDState => _gameImmersiveHUDState;

        /// <summary>
        /// Gets the current foveated rendering state, including foveation center and radii.
        /// </summary>
        public FoveatedRenderingState FoveatedRenderingState => _foveatedRenderingState;

        [DllImport("beam_eye_tracker_client")]
        private static extern IntPtr EW_BET_API_GetUserState(IntPtr trackingStateSetHandle);

        [DllImport("beam_eye_tracker_client")]
        private static extern IntPtr EW_BET_API_GetSimGameCameraState(
            IntPtr trackingStateSetHandle
        );

        [DllImport("beam_eye_tracker_client")]
        private static extern IntPtr EW_BET_API_GetGameImmersiveHUDState(
            IntPtr trackingStateSetHandle
        );

        [DllImport("beam_eye_tracker_client")]
        private static extern IntPtr EW_BET_API_GetFoveatedRenderingState(
            IntPtr trackingStateSetHandle
        );
    }

    /// <summary>
    /// Base class for receiving tracking data updates from the Beam Eye Tracker.
    /// Implement this class to receive callbacks when tracking data or status changes occur.
    /// </summary>
    /// <remarks>
    /// This class implements IDisposable to ensure proper cleanup of native resources.
    /// Users should either call Dispose explicitly or use a using statement.
    /// </remarks>
    public class TrackingListener : IDisposable
    {
        private volatile bool _isDisposed;
        internal IntPtr CallbacksHandle = IntPtr.Zero;
        internal TrackingDataReceptionStatusCallback? StatusCallback;
        internal TrackingDataCallback? DataCallback;

        /// <summary>
        /// Gets whether this listener has been disposed.
        /// </summary>
        public bool IsDisposed => _isDisposed;

        private WeakReference<API>? _owningApi;

        /// <summary>
        /// Sets the API instance that owns this listener.
        /// </summary>
        /// <param name="api">The owning API instance.</param>
        internal void SetOwningApi(API api)
        {
            _owningApi = new WeakReference<API>(api);
        }

        /// <summary>
        /// Called when the tracking data reception status changes.
        /// Override this method to handle status changes.
        /// </summary>
        /// <param name="status">The new tracking data reception status.</param>
        public virtual void OnTrackingDataReceptionStatusChanged(
            TrackingDataReceptionStatus status
        )
        { }

        /// <summary>
        /// Called when new tracking data is available.
        /// Override this method to handle tracking data updates.
        /// </summary>
        /// <param name="trackingStateSet">The new tracking state set.</param>
        /// <param name="timestamp">The timestamp of the tracking data.</param>
        public virtual void OnTrackingStateSetUpdate(
            TrackingStateSet trackingStateSet,
            double timestamp
        )
        { }

        /// <summary>
        /// Finalizer that ensures unmanaged resources are cleaned up if the instance is not properly disposed.
        /// </summary>
        ~TrackingListener()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases all resources used by this listener instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [DllImport("beam_eye_tracker_client")]
        private static extern void EW_BET_API_UnregisterTrackingCallbacks(
            IntPtr apiHandle,
            ref IntPtr callbacksHandle
        );

        /// <summary>
        /// Releases the unmanaged resources used by this listener instance and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (CallbacksHandle != IntPtr.Zero)
                {
                    API? api;
                    if (_owningApi != null && _owningApi.TryGetTarget(out api) && !api.IsDisposed)
                    {
                        try
                        {
                            api.StopReceivingTrackingDataOnListener(this);
                        }
                        catch (ObjectDisposedException)
                        {
                            // API was disposed
                        }
                    }
                    else
                    {
                        // We destroy the listener without the API handle, which is safe in the C API,
                        // but only if the C API
                        EW_BET_API_UnregisterTrackingCallbacks(IntPtr.Zero, ref CallbacksHandle);
                    }
                }
                if (CallbacksHandle != IntPtr.Zero)
                {
                    CallbacksHandle = IntPtr.Zero;
                }
                StatusCallback = null;
                DataCallback = null;
                _isDisposed = true;
                _owningApi = null;
            }
        }
    }

    /// <summary>
    /// A test class used for demonstration purposes.
    /// </summary>
    public class TestClass
    {
        /// <summary>
        /// Gets or sets the test property value.
        /// </summary>
        /// <value>
        /// An integer value that defaults to 0.
        /// </value>
        public int TestProperty { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestClass"/> class.
        /// </summary>
        public TestClass()
        {
            TestProperty = 0;
        }
    }

    /// <summary>
    /// Main entry point for the Beam Eye Tracker SDK
    /// </summary>
    /// @rst
    /// .. note:: See :ref:`about_api_singleton` for a high-level explanation of the ``API`` object.
    /// @endrst
    public class API : IDisposable
    {
        private IntPtr apiHandle = IntPtr.Zero;
        private bool disposed = false;

        /// <summary>
        /// Gets whether this API instance has been disposed.
        /// </summary>
        public bool IsDisposed => disposed;

        #region DllImports
        [DllImport("beam_eye_tracker_client")]
        private static extern int EW_BET_API_Create(
            string friendlyName,
            ViewportGeometry initialViewportGeometry,
            out IntPtr apiHandle
        );

        [DllImport("beam_eye_tracker_client")]
        private static extern void EW_BET_API_Destroy(IntPtr apiHandle);

        [DllImport("beam_eye_tracker_client")]
        private static extern void EW_BET_API_GetVersion(IntPtr apiHandle, out Version version);

        [DllImport("beam_eye_tracker_client")]
        private static extern void EW_BET_API_DestroyTrackingStateSet(
            IntPtr trackingStateSetHandle
        );

        [DllImport("beam_eye_tracker_client")]
        private static extern void EW_BET_API_UpdateViewportGeometry(
            IntPtr apiHandle,
            ViewportGeometry newViewportGeometry
        );

        [DllImport("beam_eye_tracker_client")]
        private static extern void EW_BET_API_AttemptStartingTheBeamEyeTracker(IntPtr apiHandle);

        [DllImport("beam_eye_tracker_client")]
        private static extern bool EW_BET_API_WaitForNewTrackingStateSet(
            IntPtr apiHandle,
            ref double lastReceivedTimestamp,
            uint timeoutMs
        );

        [DllImport("beam_eye_tracker_client")]
        private static extern int EW_BET_API_CreateAndFillLatestTrackingStateSet(
            IntPtr apiHandle,
            out IntPtr trackingStateSetHandle
        );

        [DllImport("beam_eye_tracker_client")]
        private static extern TrackingDataReceptionStatus EW_BET_API_GetTrackingDataReceptionStatus(
            IntPtr apiHandle
        );

        [DllImport("beam_eye_tracker_client")]
        private static extern IntPtr EW_BET_API_GetUserState(IntPtr trackingStateSetHandle);

        [DllImport("beam_eye_tracker_client")]
        private static extern IntPtr EW_BET_API_GetSimGameCameraState(
            IntPtr trackingStateSetHandle
        );

        [DllImport("beam_eye_tracker_client")]
        private static extern IntPtr EW_BET_API_GetGameImmersiveHUDState(
            IntPtr trackingStateSetHandle
        );

        [DllImport("beam_eye_tracker_client")]
        private static extern IntPtr EW_BET_API_GetFoveatedRenderingState(
            IntPtr trackingStateSetHandle
        );

        [DllImport("beam_eye_tracker_client")]
        private static extern int EW_BET_API_RegisterTrackingCallbacks(
            IntPtr apiHandle,
            TrackingDataReceptionStatusCallback statusCallback,
            TrackingDataCallback dataCallback,
            IntPtr userData,
            out IntPtr callbacksHandle
        );

        [DllImport("beam_eye_tracker_client")]
        private static extern void EW_BET_API_UnregisterTrackingCallbacks(
            IntPtr apiHandle,
            ref IntPtr callbacksHandle
        );

        [DllImport("beam_eye_tracker_client")]
        private static extern SimCameraTransform3D EW_BET_API_ComputeSimGameCameraTransformParameters(
            SimGameCameraState cameraState,
            float eyeTrackingWeight,
            float headTrackingWeight
        );

        [DllImport("beam_eye_tracker_client")]
        private static extern bool EW_BET_API_RecenterSimGameCameraStart(IntPtr apiHandle);

        [DllImport("beam_eye_tracker_client")]
        private static extern void EW_BET_API_RecenterSimGameCameraEnd(IntPtr apiHandle);
        #endregion

        /// <summary>
        /// Default timeout in milliseconds for waiting for new tracking data.
        /// </summary>
        public const uint DefaultTrackingDataTimeoutMs = 1000;

        /// <summary>
        /// Initializes a new instance of the Beam Eye Tracker API.
        /// </summary>
        /// <param name="friendlyName">Application identifier displayed in the Beam Eye Tracker UI (UTF-8, max
        /// 200 bytes)</param>
        /// <param name="initialViewportGeometry">The initial viewport geometry configuration.</param>
        /// <exception cref="ArgumentNullException">Thrown when friendlyName is null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when friendlyName exceeds 200 bytes.</exception>
        /// <exception cref="Exception">Thrown when API creation fails.</exception>
        public API(string friendlyName, ViewportGeometry initialViewportGeometry)
        {
            if (string.IsNullOrEmpty(friendlyName))
                throw new ArgumentNullException(nameof(friendlyName));

            if (friendlyName.Length > 200)
                throw new ArgumentException(
                    "Friendly name must not exceed 200 bytes",
                    nameof(friendlyName)
                );

            int result = EW_BET_API_Create(friendlyName, initialViewportGeometry, out apiHandle);
            if (result != 0)
                throw new Exception($"Failed to create Beam Eye Tracker API. Error code: {result}");
        }

        /// <summary>
        /// Gets the version information of the Beam Eye Tracker SDK.
        /// </summary>
        /// <returns>A Version structure containing major, minor, and patch version numbers.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the API instance has been disposed.</exception>
        public Version GetVersion()
        {
            ThrowIfDisposed();
            EW_BET_API_GetVersion(apiHandle, out Version version);
            return version;
        }

        /// <summary>
        /// Updates the viewport geometry used for coordinate mapping.
        /// </summary>
        /// <param name="newViewportGeometry">The new viewport geometry configuration.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the API instance has been disposed.</exception>
        /// @rst
        /// .. note::
        ///     See :ref:`viewport` for more information about viewport geometry.
        /// @endrst
        public void UpdateViewportGeometry(ViewportGeometry newViewportGeometry)
        {
            ThrowIfDisposed();
            EW_BET_API_UpdateViewportGeometry(apiHandle, newViewportGeometry);
        }

        /// <summary>
        /// Attempts to automatically start the Beam Eye Tracker application if it's not already running.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the API instance has been disposed.</exception>
        /// @rst
        /// .. note::
        ///     See :ref:`about_auto_start_tracking` for more information.
        /// @endrst
        public void AttemptStartingTheBeamEyeTracker()
        {
            ThrowIfDisposed();
            EW_BET_API_AttemptStartingTheBeamEyeTracker(apiHandle);
        }

        /// <summary>
        /// Waits for new tracking data to become available.
        /// </summary>
        /// <param name="lastReceivedTimestamp">The timestamp of the last received data. Will be updated with the new timestamp if new data is available.</param>
        /// <param name="timeoutMs">Maximum time to wait in milliseconds. Defaults to DefaultTrackingDataTimeoutMs.</param>
        /// <returns>True if new data is available, false if the timeout was reached.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the API instance has been disposed.</exception>
        /// @rst
        /// .. note::
        ///     See :ref:`about_synchronous_data_access` and :ref:`about_polling_data_access` for more information.
        /// @endrst
        public bool WaitForNewTrackingData(
            ref double lastReceivedTimestamp,
            uint timeoutMs = DefaultTrackingDataTimeoutMs
        )
        {
            ThrowIfDisposed();
            return EW_BET_API_WaitForNewTrackingStateSet(
                apiHandle,
                ref lastReceivedTimestamp,
                timeoutMs
            );
        }

        /// <summary>
        /// Gets the current status of tracking data reception.
        /// </summary>
        /// <returns>The current TrackingDataReceptionStatus.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the API instance has been disposed.</exception>
        /// @rst
        /// .. note::
        ///     See :ref:`about_tracking_data_reception_status` for more information.
        /// @endrst
        public TrackingDataReceptionStatus GetTrackingDataReceptionStatus()
        {
            ThrowIfDisposed();
            return EW_BET_API_GetTrackingDataReceptionStatus(apiHandle);
        }

        /// <summary>
        /// Gets the most recent tracking state set containing user state, camera state, and HUD state.
        /// </summary>
        /// <returns>A TrackingStateSet containing the latest tracking data.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the API instance has been disposed.</exception>
        /// <exception cref="Exception">Thrown when retrieving the tracking state set fails.</exception>
        /// @rst
        /// .. note::
        ///     See :ref:`about_tracking_state_set` for more information.
        /// @endrst
        public TrackingStateSet GetLatestTrackingStateSet()
        {
            ThrowIfDisposed();

            int result = EW_BET_API_CreateAndFillLatestTrackingStateSet(
                apiHandle,
                out IntPtr trackingStateSetHandle
            );

            if (result != 0)
                throw new Exception(
                    $"Failed to get latest tracking state set. Error code: {result}"
                );

            try
            {
                return new TrackingStateSet(trackingStateSetHandle);
            }
            finally
            {
                EW_BET_API_DestroyTrackingStateSet(trackingStateSetHandle);
            }
        }

        /// <summary>
        /// Registers a listener to receive tracking data updates.
        /// </summary>
        /// <param name="listener">The TrackingListener to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when listener is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the listener is already registered.</exception>
        /// <exception cref="Exception">Thrown when registering the callbacks fails.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the API instance has been disposed.</exception>
        /// @rst
        /// .. note:: For a detailed explanation, see :ref:`about_asynchronous_data_access`.
        /// @endrst
        public void StartReceivingTrackingDataOnListener(TrackingListener listener)
        {
            ThrowIfDisposed();
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            if (listener.CallbacksHandle != IntPtr.Zero)
                throw new InvalidOperationException("This listener is already registered");

            // Allows to release the listener without the API handle if the API is disposed first.
            listener.SetOwningApi(this);

            listener.StatusCallback = (TrackingDataReceptionStatus status, IntPtr userData) =>
            {
                if (!listener.IsDisposed)
                {
                    try
                    {
                        listener.OnTrackingDataReceptionStatusChanged(status);
                    }
                    catch (ObjectDisposedException) { }
                }
            };

            listener.DataCallback = (
                IntPtr trackingStateSetHandle,
                double timestamp,
                IntPtr userData
            ) =>
            {
                if (!listener.IsDisposed)
                {
                    try
                    {
                        listener.OnTrackingStateSetUpdate(
                            new TrackingStateSet(trackingStateSetHandle),
                            timestamp
                        );
                    }
                    catch (ObjectDisposedException) { }
                }
            };

            int result = EW_BET_API_RegisterTrackingCallbacks(
                apiHandle,
                listener.StatusCallback,
                listener.DataCallback,
                IntPtr.Zero,
                out listener.CallbacksHandle
            );

            if (result != 0)
                throw new Exception($"Failed to register tracking callbacks. Error code: {result}");
        }

        /// <summary>
        /// Unregisters a previously registered tracking data listener.
        /// </summary>
        /// <param name="listener">The TrackingListener to unregister.</param>
        /// <exception cref="ArgumentNullException">Thrown when listener is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the listener is not currently registered.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the API instance has been disposed.</exception>
        public void StopReceivingTrackingDataOnListener(TrackingListener listener)
        {
            ThrowIfDisposed();
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            if (listener.CallbacksHandle == IntPtr.Zero)
                throw new ArgumentException("The provided listener is not currently registered");

            EW_BET_API_UnregisterTrackingCallbacks(apiHandle, ref listener.CallbacksHandle);
            listener.StatusCallback = null;
            listener.DataCallback = null;
        }

        /// <summary>
        /// Computes the final camera transform parameters by combining eye and head tracking components.
        /// </summary>
        /// <param name="cameraState">The current camera state containing eye and head tracking components.</param>
        /// <param name="eyeTrackingWeight">Weight factor for the eye tracking component (default: 1.0).</param>
        /// <param name="headTrackingWeight">Weight factor for the head tracking component (default: 1.0).</param>
        /// <returns>The computed camera transform parameters.</returns>
        /// @rst
        /// .. note:: See :ref:`about_sim_game_camera_state` for more information.
        /// @endrst
        public static SimCameraTransform3D ComputeSimGameCameraTransformParameters(
            SimGameCameraState cameraState,
            float eyeTrackingWeight = 1.0f,
            float headTrackingWeight = 1.0f
        )
        {
            return EW_BET_API_ComputeSimGameCameraTransformParameters(
                cameraState,
                eyeTrackingWeight,
                headTrackingWeight
            );
        }

        /// <summary>
        /// Starts the process of recentering the game camera.
        /// </summary>
        /// <returns>True if recentering was successfully started, false otherwise.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the API instance has been disposed.</exception>
        /// @rst
        /// .. note:: See :ref:`about_camera_recentering` for more information.
        /// @endrst
        public bool StartRecenterSimGameCamera()
        {
            ThrowIfDisposed();
            return EW_BET_API_RecenterSimGameCameraStart(apiHandle);
        }

        /// <summary>
        /// Stops the process of recentering the game camera.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the API instance has been disposed.</exception>
        /// @rst
        /// .. note:: See :ref:`about_camera_recentering` for more information.
        /// @endrst
        public void StopRecenterSimGameCamera()
        {
            ThrowIfDisposed();
            EW_BET_API_RecenterSimGameCameraEnd(apiHandle);
        }

        /// <summary>
        /// Throws an ObjectDisposedException if this API instance has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the API instance has been disposed.</exception>
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(API));
        }

        /// <summary>
        /// Releases all resources used by this API instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by this API instance and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // No managed resources to dispose in this case.
                }

                // Dispose unmanaged resources
                if (apiHandle != IntPtr.Zero)
                {
                    EW_BET_API_Destroy(apiHandle);
                    apiHandle = IntPtr.Zero;
                }

                disposed = true;
            }
        }

        /// <summary>
        /// Finalizer that ensures unmanaged resources are cleaned up if the instance is not properly disposed.
        /// </summary>
        ~API()
        {
            Dispose(false);
        }
    }
}
