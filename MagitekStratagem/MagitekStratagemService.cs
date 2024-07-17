using System;
using System.Collections.Generic;
using Tobii.GameIntegration.Net;

namespace MagitekStratagemPlugin
{
  public sealed class Point
  {
    public float X { get; set; }
    public float Y { get; set; }

    public Point(float x, float y)
    {
      X = x;
      Y = y;
    }

    internal Point()
    {
    }
  }

  public sealed class CalibrationPoint
  {
    public Point? Reference { get; set; }

    public Point? Gaze { get; set; }

    public Point? Delta { get; set; }

    public CalibrationPoint(float rx, float ry, float gx, float gy)
    {
      Reference = new Point(rx, ry);
      Gaze = new Point(gx, gy);
      Delta = new Point(gx - rx, gy - ry);
    }

    public CalibrationPoint(Point reference, Point gaze)
    {
      Reference = reference;
      Gaze = gaze;
      Delta = new Point(gaze.X - reference.X, gaze.Y - reference.Y);
    }
    internal CalibrationPoint()
    {
    }
  }

  public sealed class TobiiService : ITrackerService
  {
    public bool IsTracking { get; private set; }
    public long LastGazeTimeStamp { get; private set; }
    public float LastGazeX { get; private set; }
    public float LastGazeY { get; private set; }
    public float LastRawGazeX { get; private set; }
    public float LastRawGazeY { get; private set; }
    public long LastHeadTimeStamp { get; private set; }
    public float LastHeadPositionX { get; private set; }
    public float LastHeadPositionY { get; private set; }
    public float LastHeadPositionZ { get; private set; }
    public float LastHeadRotationPitch { get; private set; }
    public float LastHeadRotationYaw { get; private set; }
    public float LastHeadRotationRoll { get; private set; }
    public Transformation ExtendedTransform { get; private set; }

    public List<CalibrationPoint> CalibrationPoints { get; private set; }

    public bool UseCalibration { get; set; }

    public TobiiService(List<CalibrationPoint> calibrationPoints)
    {
      TobiiGameIntegrationApi.SetApplicationName("FFXIV Magitek Stratagem");
      var tinfos = TobiiGameIntegrationApi.GetTrackerInfos();

      foreach (var tinfo in tinfos)
      {
        Service.PluginLog.Verbose($"Tracker: {tinfo.SerialNumber}: {tinfo.Type} {tinfo.FriendlyName} {tinfo.Url} {tinfo.FirmwareVersion}");
      }

      TobiiGameIntegrationApi.PrelinkAll();
      if (TobiiGameIntegrationApi.IsApiInitialized())
      {
        Service.PluginLog.Verbose($"Tobii Game Integration API Initialized.");
      }
      else
      {
        Service.PluginLog.Verbose($"Tobii Game Integration API NOT Initialized.");
      }
      if (TobiiGameIntegrationApi.IsPresent())
      {
        Service.PluginLog.Verbose($"Tobii Eye Tracker is present.");
      }
      else
      {
        Service.PluginLog.Verbose($"Tobii Eye Tracker is NOT present.");
      }
      if (TobiiGameIntegrationApi.IsTrackerConnected())
      {
        Service.PluginLog.Verbose($"Tobii Eye Tracker is connected.");
      }
      else
      {
        Service.PluginLog.Verbose($"Tobii Eye Tracker is NOT connected.");
      }
      if (TobiiGameIntegrationApi.IsTrackerEnabled())
      {
        Service.PluginLog.Verbose($"Tobii Eye Tracker is enabled.");
      }
      else
      {
        Service.PluginLog.Verbose($"Tobii Eye Tracker is NOT enabled.");
      }
      TobiiGameIntegrationApi.UpdateTrackerInfos();
      CalibrationPoints = calibrationPoints;
    }

    public List<TrackerInfo> GetTrackerInfos()
    {
      return TobiiGameIntegrationApi.GetTrackerInfos();
    }

    public void StartTrackingWindow(nint windowHandle)
    {
      if (!IsTracking)
      {
        Service.PluginLog.Verbose($"Attempting to track window #{windowHandle}.");
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
        LastRawGazeX = gazePoint.X;
        LastRawGazeY = gazePoint.Y;

        if (CalibrationPoints?.Count > 0)
        {
          var calibratedGazePoint = ApplyCalibrationPoints(gazePoint.X, gazePoint.Y);
          LastGazeX = calibratedGazePoint.X;
          LastGazeY = calibratedGazePoint.Y;
        }
        else
        {
          LastGazeX = gazePoint.X;
          LastGazeY = gazePoint.Y;
        }

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

    public void AddCalibrationPoint(float x, float y)
    {
      var calibrationPoint = new CalibrationPoint(x, y, LastRawGazeX, LastRawGazeY);
      CalibrationPoints?.Add(calibrationPoint);
    }

    private Point ApplyCalibrationPoints(float gazeX, float gazeY)
    {
      // Apply calibration points to the gaze point.
      // The farther the gaze point is from the calibration point, the less it will be affected.

      var gazePoint = new Point(gazeX, gazeY);

      if (CalibrationPoints != null && UseCalibration)
      {
        var totalWeight = 0d;
        var totalDeltaX = 0d;
        var totalDeltaY = 0d;
        foreach (var calibrationPoint in CalibrationPoints)
        {
          if (calibrationPoint.Reference == null || calibrationPoint.Gaze == null || calibrationPoint.Delta == null)
          {
            continue;
          }

          var distance = Math.Pow(Math.Pow(gazePoint.X - calibrationPoint.Reference.X, 2) + Math.Pow(gazePoint.Y - calibrationPoint.Reference.Y, 2), 0.5);
          var weight = 1 / (1 + distance);

          totalWeight += weight;
          totalDeltaX -= calibrationPoint.Delta.X * weight;
          totalDeltaY -= calibrationPoint.Delta.Y * weight;
        }

        gazePoint.X += (float)(totalDeltaX / totalWeight);
        gazePoint.Y += (float)(totalDeltaY / totalWeight);
      }

      return gazePoint;
    }
  }
}
