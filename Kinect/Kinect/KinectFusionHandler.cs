using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.Fusion;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kinect {
  class KinectFusionHandler {

    /// <summary>
    /// Max tracking error count, we will reset the reconstruction if tracking errors
    /// reach this number
    /// </summary>
    private const int MaxTrackingErrors = 100;

    /// <summary>
    /// If set true, will automatically reset the reconstruction when MaxTrackingErrors have occurred
    /// </summary>
    private const bool AutoResetReconstructionWhenLost = false;

    /// <summary>
    /// The resolution of the depth image to be processed.
    /// </summary>
    private const DepthImageFormat DepthImageResolution = DepthImageFormat.Resolution640x480Fps30;

    /// <summary>
    /// The seconds interval to calculate FPS
    /// </summary>
    private const int FpsInterval = 10;

    /// <summary>
    /// The reconstruction volume voxel density in voxels per meter (vpm)
    /// 1000mm / 256vpm = ~3.9mm/voxel
    /// </summary>
    private const int VoxelsPerMeter = 64;

    /// <summary>
    /// The reconstruction volume voxel resolution in the X axis
    /// At a setting of 256vpm the volume is 512 / 256 = 2m wide
    /// </summary>
    private const int VoxelResolutionX = 128;

    /// <summary>
    /// The reconstruction volume voxel resolution in the Y axis
    /// At a setting of 256vpm the volume is 384 / 256 = 1.5m high
    /// </summary>
    private const int VoxelResolutionY = 128;

    /// <summary>
    /// The reconstruction volume voxel resolution in the Z axis
    /// At a setting of 256vpm the volume is 512 / 256 = 2m deep
    /// </summary>
    private const int VoxelResolutionZ = 128;

    /// <summary>
    /// The reconstruction volume processor type. This parameter sets whether AMP or CPU processing
    /// is used. Note that CPU processing will likely be too slow for real-time processing.
    /// </summary>
    private const ReconstructionProcessor ProcessorType = ReconstructionProcessor.Amp;

    /// <summary>
    /// The zero-based device index to choose for reconstruction processing if the 
    /// ReconstructionProcessor AMP options are selected.
    /// Here we automatically choose a device to use for processing by passing -1, 
    /// </summary>
    private const int DeviceToUse = -1;

    /// <summary>
    /// Parameter to translate the reconstruction based on the minimum depth setting. When set to
    /// false, the reconstruction volume +Z axis starts at the camera lens and extends into the scene.
    /// Setting this true in the constructor will move the volume forward along +Z away from the
    /// camera by the minimum depth threshold to enable capture of very small reconstruction volumes
    /// by setting a non-identity world-volume transformation in the ResetReconstruction call.
    /// Small volumes should be shifted, as the Kinect hardware has a minimum sensing limit of ~0.35m,
    /// inside which no valid depth is returned, hence it is difficult to initialize and track robustly  
    /// when the majority of a small volume is inside this distance.
    /// </summary>
    private bool translateResetPoseByMinDepthThreshold = true;

    /// <summary>
    /// Minimum depth distance threshold in meters. Depth pixels below this value will be
    /// returned as invalid (0). Min depth must be positive or 0.
    /// </summary>
    private float minDepthClip = FusionDepthProcessor.DefaultMinimumDepth;

    /// <summary>
    /// Maximum depth distance threshold in meters. Depth pixels above this value will be
    /// returned as invalid (0). Max depth must be greater than 0.
    /// </summary>
    private float maxDepthClip = FusionDepthProcessor.DefaultMaximumDepth;

    /// <summary>
    /// Active Kinect sensor
    /// </summary>
    private KinectSensor sensor;

    /// <summary>
    /// Intermediate storage for the depth data converted to color
    /// </summary>
    private int[] colorPixels;

    /// <summary>
    /// Intermediate storage for the depth float data converted from depth image frame
    /// </summary>
    private FusionFloatImageFrame depthFloatBuffer;

    /// <summary>
    /// Intermediate storage for the point cloud data converted from depth float image frame
    /// </summary>
    private FusionPointCloudImageFrame pointCloudBuffer;

    /// <summary>
    /// Raycast shaded surface image
    /// </summary>
    private FusionColorImageFrame shadedSurfaceColorFrame;

    /// <summary>
    /// The transformation between the world and camera view coordinate system
    /// </summary>
    private Matrix4 worldToCameraTransform;

    /// <summary>
    /// The default transformation between the world and volume coordinate system
    /// </summary>
    private Matrix4 defaultWorldToVolumeTransform;

    private Matrix4 volumeTransform;

    /// <summary>
    /// The Kinect Fusion volume
    /// </summary>
    private Reconstruction volume;

    /// <summary>
    /// The count of the frames processed in the FPS interval
    /// </summary>
    private int processedFrameCount;

    /// <summary>
    /// The tracking error count
    /// </summary>
    private int trackingErrorCount;

    /// <summary>
    /// The sensor depth frame data length
    /// </summary>
    private int frameDataLength;

    /// <summary>
    /// The count of the depth frames to be processed
    /// </summary>
    private bool processingFrame;

    /// <summary>
    /// Track whether Dispose has been called
    /// </summary>
    private bool disposed;

    public Matrix4 Transform {
      get {
        return worldToCameraTransform;
      }
    }

    public Matrix4 VolumeTransform {
      get {
        return volumeTransform;
      }
    }

    public float Density {
      get {
        return VoxelsPerMeter;
      }
    }

    /// <summary>
    /// Execute startup tasks
    /// </summary>
    /// <param name="sender">object sending the event</param>
    /// <param name="e">event arguments</param>
    public void Initialize(KinectSensor sensor) {
      this.sensor = sensor;

      this.frameDataLength = this.sensor.DepthStream.FramePixelDataLength;

      // Allocate space to put the color pixels we'll create
      this.colorPixels = new int[this.frameDataLength];

      var volParam = new ReconstructionParameters(VoxelsPerMeter, VoxelResolutionX, VoxelResolutionY, VoxelResolutionZ);

      // Set the world-view transform to identity, so the world origin is the initial camera location.
      this.worldToCameraTransform = Matrix4.Identity;

      try {
        // This creates a volume cube with the Kinect at center of near plane, and volume directly
        // in front of Kinect.
        this.volume = Reconstruction.FusionCreateReconstruction(volParam, ProcessorType, DeviceToUse, this.worldToCameraTransform);

        this.defaultWorldToVolumeTransform = this.volume.GetCurrentWorldToVolumeTransform();
        

      } catch (InvalidOperationException ex) {
        return;
      } catch (DllNotFoundException) {
        return;
      }

      // Depth frames generated from the depth input
      this.depthFloatBuffer = new FusionFloatImageFrame((int)320, (int)240);

      // Point cloud frames generated from the depth float input
      this.pointCloudBuffer = new FusionPointCloudImageFrame((int)320, (int)240);

      // Create images to raycast the Reconstruction Volume
      this.shadedSurfaceColorFrame = new FusionColorImageFrame((int)240, (int)240);
    }

    public void RouteToFusion(DepthImagePixel[] depthPixels) {
      DepthImagePixel[] pixels = depthPixels;
      /*
      if (!this.processingFrame) {
        this.processingFrame = true;
        Thread thread = new Thread(() => ProcessDepthData(pixels));
        thread.Start();
      }*/
      ProcessDepthData(pixels);
    }

    /// <summary>
    /// Process the depth input
    /// </summary>
    /// <param name="depthPixels">The depth data array to be processed</param>
    private void ProcessDepthData(DepthImagePixel[] depthPixels) {
      try {
        // Convert the depth image frame to depth float image frame
        FusionDepthProcessor.DepthToDepthFloatFrame(
            depthPixels,
            (int)320,
            (int)240,
            this.depthFloatBuffer,
            FusionDepthProcessor.DefaultMinimumDepth,
            FusionDepthProcessor.DefaultMaximumDepth,
            false);

        // ProcessFrame will first calculate the camera pose and then integrate
        // if tracking is successful
        bool trackingSucceeded = this.volume.ProcessFrame(
            this.depthFloatBuffer,
            FusionDepthProcessor.DefaultAlignIterationCount,
            FusionDepthProcessor.DefaultIntegrationWeight,
            this.volume.GetCurrentWorldToCameraTransform());

        

        // If camera tracking failed, no data integration or raycast for reference
        // point cloud will have taken place, and the internal camera pose
        // will be unchanged.
        if (!trackingSucceeded) {
          this.trackingErrorCount++;
          if (this.trackingErrorCount == MaxTrackingErrors) {
            //ResetReconstruction();
          }
        } else {
          Matrix4 calculatedCameraPose = this.volume.GetCurrentWorldToCameraTransform();
         /* Debug.WriteLine("[[{0}, {1}, {2}, {3}],[{4}, {5}, {6}, {7}],[{8}, {9}, {10}, {11}],[{12}, {13}, {14}, {15}],]",
            calculatedCameraPose.M11, calculatedCameraPose.M12, calculatedCameraPose.M13, calculatedCameraPose.M14,
            calculatedCameraPose.M21, calculatedCameraPose.M22, calculatedCameraPose.M23, calculatedCameraPose.M24,
            calculatedCameraPose.M31, calculatedCameraPose.M32, calculatedCameraPose.M33, calculatedCameraPose.M34,
            calculatedCameraPose.M41, calculatedCameraPose.M42, calculatedCameraPose.M43, calculatedCameraPose.M44
            );*/

          // Set the camera pose and reset tracking errors
          this.worldToCameraTransform = calculatedCameraPose;

          this.trackingErrorCount = 0;
        }

        ++this.processedFrameCount;
      }finally {
        this.processingFrame = false;
      }
    }

    /// <summary>
    /// Reset the reconstruction to initial value
    /// </summary>
    private void ResetReconstruction() {
      // Reset tracking error counter
      this.trackingErrorCount = 0;

      // Set the world-view transform to identity, so the world origin is the initial camera location.
      this.worldToCameraTransform = Matrix4.Identity;

      if (null != this.volume) {
        // Translate the reconstruction volume location away from the world origin by an amount equal
        // to the minimum depth threshold. This ensures that some depth signal falls inside the volume.
        // If set false, the default world origin is set to the center of the front face of the 
        // volume, which has the effect of locating the volume directly in front of the initial camera
        // position with the +Z axis into the volume along the initial camera direction of view.
        if (this.translateResetPoseByMinDepthThreshold) {
          Matrix4 worldToVolumeTransform = this.defaultWorldToVolumeTransform;

          // Translate the volume in the Z axis by the minDepthThreshold distance
          float minDist = (this.minDepthClip < this.maxDepthClip) ? this.minDepthClip : this.maxDepthClip;
          worldToVolumeTransform.M43 -= minDist * VoxelsPerMeter;

          this.volume.ResetReconstruction(this.worldToCameraTransform, worldToVolumeTransform);
        } else {
          this.volume.ResetReconstruction(this.worldToCameraTransform);
        }
      }
    }
  }
}
