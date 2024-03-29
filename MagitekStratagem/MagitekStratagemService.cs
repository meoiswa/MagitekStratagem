using System.Collections.Generic;
using Dalamud.Logging;
using Tobii.GameIntegration.Net;

namespace MagitekStratagemPlugin
{
    public sealed class TobiiService : ITrackerService
  {
    public bool IsTracking { get; private set; }
    public long LastGazeTimeStamp { get; private set; }
    public float LastGazeX { get; private set; }
    public float LastGazeY { get; private set; }
    public long LastHeadTimeStamp { get; private set; }
    public float LastHeadPositionX { get; private set; }
    public float LastHeadPositionY { get; private set; }
    public float LastHeadPositionZ { get; private set; }
    public float LastHeadRotationPitch { get; private set; }
    public float LastHeadRotationYaw { get; private set; }
    public float LastHeadRotationRoll { get; private set; }
    public Transformation ExtendedTransform { get; private set; }

    public TobiiService()
    {
      TobiiGameIntegrationApi.SetApplicationName("FFXIV Magitek Stratagem");
      TobiiGameIntegrationApi.IsApiInitialized();
      TobiiGameIntegrationApi.UpdateTrackerInfos();
    }

    public List<TrackerInfo> GetTrackerInfos()
    {
      return TobiiGameIntegrationApi.GetTrackerInfos();
    }

    public void StartTrackingWindow(nint windowHandle)
    {
      if (!IsTracking)
      {
        if (TobiiGameIntegrationApi.TrackWindow(windowHandle))
        {
          IsTracking = true;
          Service.PluginLog.Debug("Tracking Window.");
        }
        else
        {
          Service.PluginLog.Debug("Failed to track window.");
        }
      }
    }

    public void StopTracking()
    {
      if (IsTracking)
      {
        TobiiGameIntegrationApi.StopTracking();
        IsTracking = false;
      }
    }

    public void Update()
    {
      TobiiGameIntegrationApi.Update();

      TobiiGameIntegrationApi.TryGetLatestGazePoint(out GazePoint gazePoint);

      if (gazePoint.TimeStampMicroSeconds > LastGazeTimeStamp)
      {
        LastGazeX = gazePoint.X;
        LastGazeY = gazePoint.Y;
        LastGazeTimeStamp = gazePoint.TimeStampMicroSeconds;
      }

      TobiiGameIntegrationApi.TryGetLatestHeadPose(out HeadPose headPose);

      if (headPose.TimeStampMicroSeconds > LastHeadTimeStamp)
      {
        LastHeadPositionX = headPose.Position.X;
        LastHeadPositionY = headPose.Position.Y;
        LastHeadPositionZ = headPose.Position.Z;
        LastHeadRotationPitch = headPose.Rotation.PitchDegrees;
        LastHeadRotationYaw = headPose.Rotation.YawDegrees;
        LastHeadRotationRoll = headPose.Rotation.RollDegrees;
        LastHeadTimeStamp = headPose.TimeStampMicroSeconds;
      }

      ExtendedTransform = TobiiGameIntegrationApi.GetExtendedViewTransformation();
    }

    public void Shutdown()
    {
      StopTracking();
      TobiiGameIntegrationApi.Shutdown();
    }
  }
}
