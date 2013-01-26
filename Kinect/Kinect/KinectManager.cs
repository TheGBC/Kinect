using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using System.Threading;

namespace KinectSample {
  class KinectManager {
    private KinectMode mode;
    private KinectSensor sensor = null;

    // Xna Image
    private uint[] XnaImageData = null;

    // Depth data
    private ushort[] depthData = null;

    // The current plane
    private Plane plane = null;

    // The points belonging to the current plane
    private List<Vector2> planePoints = null;
    private List<Vector3> depthPlanePoints = null;

    // The current joint positions
    private List<Vector2> jointPoints = null;

    // The current joints
    private List<Joint> joints = null;


    // Locks to ensure that
    //   a. The asynchronous methods do not pile up
    //   b. Other resources can not access data that is in the middle of
    //        being processed
    private bool colorUpdateReady = true;
    private bool depthUpdateReady = true;
    private bool skeletonUpdateReady = true;

    private object imageLock = new object();
    private object depthLock = new object();
    private object skeletonLock = new object();


    // Constants
    private readonly int WIDTH = 640;
    private readonly int HEIGHT = 480;
    private readonly int MAX_DEPTH = 8191;


    public KinectManager(KinectMode mode) {
      this.mode = mode;

      // Get at least one kinect sensor
      foreach (KinectSensor potentialSensor in KinectSensor.KinectSensors) {
        if (potentialSensor.Status == KinectStatus.Connected) {
          sensor = potentialSensor;
          break;
        }
      }

      if (sensor != null) {
        // Enable Depth
        if (DepthEnabled) {
          sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
          sensor.DepthFrameReady += OnDepthFrameReady;
        }

        // Enable Video
        if (VideoEnabled) {
          sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
          sensor.ColorFrameReady += OnColorFrameReady;
        }

        // Enable Skeleton
        if (SkeletonEnabled) {
          sensor.SkeletonStream.Enable();
          sensor.SkeletonFrameReady += OnSkeletonFrameReady;
        }
      }
    }

    // Start the sensor
    public void Start() {
      if (sensor != null && !IsRunning) {
        sensor.Start();
      }
    }

    // When a depth frame is ready
    private void OnDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e) {
      if (depthUpdateReady) {
        using (DepthImageFrame depthFrame = e.OpenDepthImageFrame()) {

          if (depthFrame != null) {

            // Drop updates if they start to pile up
            depthUpdateReady = false;

            Monitor.Enter(depthLock);

            if (planePoints != null) {
              planePoints.Clear();
            } else {
              planePoints = new List<Vector2>();
            }

            if (depthPlanePoints != null) {
              depthPlanePoints.Clear();
            } else {
              depthPlanePoints = new List<Vector3>();
            }


            short[] rawData = new short[depthFrame.PixelDataLength];
            depthFrame.CopyPixelDataTo(rawData);
            depthData = formatShortData(rawData);


            EnhancedAlgorithm();

            depthUpdateReady = true;

            Monitor.Exit(depthLock);
          }
        }
      }
    }

    // Return a unique hash number given a coordinate pair
    private int pointHash(int x, int y) {
      return y * WIDTH + x;
    }

    //
    // This is assuming that the ground will be, for the most part, down
    //   Thus, points closer to the bottom will be more likely to be part of the ground plane than
    //   those above it. Thus, this algorithm will detect an incorrect plane if the kinect is tilted
    //   to such a degree where the ground is almost not visible or not the lowest plane (If the kinect
    //   is upside down, for example)
    //
    // Consider the 3d axis system the point cloud exists in.
    //   x and y can be thought of as independent and z dependent, such that
    //   the function for depth is f(x, y).
    //   This makes sense because for a unique pair (x, y), there is only one value z
    //
    //   Instead, let's think about y as a function of x and z
    //     x and z should only map to one y, but this will not always be the case
    //     If we are only interested in the bottom y values (those that are most likely ground),
    //     we won't have to consider coordinates above it.
    //     Therefore, we can have x and z map to the highest (lowest altitude) y values
    //
    // We'll keep track of which x z combinations we have by hashing
    //   each coordinate pair to the unique integer hash number (z * MAX_X_VALUE) + x.
    //
    // If accessing the set is constant time, then finding the ground points will run in O(n)
    //   for n points sorted with respect to y. 
    private void EnhancedAlgorithm() {
      HashSet<int> seenValues = new HashSet<int>();

      // List of all the points
      Vector3[] allPoints = new Vector3[WIDTH * HEIGHT];
      List<Vector3> lowerSet = new List<Vector3>();

      // Start from the bottom up, selecting the points most likely on the ground
      for (int y = HEIGHT - 1; y >= 0; y--) {
        for (int x = 0; x < WIDTH; x++) {
          int hashIndex = pointHash(x, getDepth(x, y));
          Vector3 v = new Vector3(x, y, getDepth(x, y));
          allPoints[pointHash(x, y)] = v;

          // Points with a depth of the max depth can very well be unneccessary noise
          //
          // If we've encountered an (x, z) pair that maps to a higher (lower altitude) y, we
          //   can ignore it.
          if (seenValues.Contains(hashIndex) || getDepth(x, y) == MAX_DEPTH) {
            continue;
          } else {
            seenValues.Add(hashIndex);
            lowerSet.Add(v);
            //depthPlanePoints.Add(v);
          }
        }
      }
      
      ulong squareValue = ulong.MaxValue;
      int iterations = 10;
      int count = 0;
      Random randomGenerator = new Random();
      Plane plane = null;

      // We can't build a plane with less than 3 points
      if (lowerSet.Count < 3) {
        return;
      }

      while (count < iterations) {
        // Get three random points from the lowerSet
        ulong currentSquares = 0;
        Vector3[] points = selectRandomPoints(lowerSet, randomGenerator);
        Vector3 v1 = points[0] - points[1];
        Vector3 v2 = points[0] - points[2];
        Vector3 p = points[0];
        Plane planeCandidate = new Plane(v1, v2, p);
        foreach (Vector3 point in lowerSet) {
          double dist = planeCandidate.getDistance(point);
          currentSquares += (ulong)(dist * dist);
        }

        if (currentSquares > squareValue) {
          count++;
        } else {
          squareValue = currentSquares;
          plane = planeCandidate;
          count = 0;
        }
      }

      foreach(Vector3 p in allPoints){
        if (Math.Abs(plane.getDistance(p)) < 5) {
          depthPlanePoints.Add(p);
        }
      }
    }

    // Returns three random points selected from the list
    private Vector3[] selectRandomPoints(List<Vector3> points, Random generator) {
      Vector3[] randList = new Vector3[3];
      for (int i = 0; i < 3; i++) {
        int ind = generator.Next(points.Count - i) + i;
        randList[i] = points[ind];
        points[ind] = points[i];
      }
      return randList;
    }

    private Vector3 normalVector(Vector3 point, Vector3 up, Vector3 down, Vector3 left, Vector3 right) {
      // Get the vectors of all the points
      Vector3 u = VectorMath.Subtract(point, up);
      Vector3 d = VectorMath.Subtract(point, down);
      Vector3 l = VectorMath.Subtract(point, left);
      Vector3 r = VectorMath.Subtract(point, right);

      // The four normal vectors calculatable from the above
      Vector3 cpUL = VectorMath.CrossProduct(u, l);
      Vector3 cpUR = VectorMath.CrossProduct(u, r);
      Vector3 cpDL = VectorMath.CrossProduct(d, l);
      Vector3 cpDR = VectorMath.CrossProduct(d, r);

      // Return the average of the four normal vectors
      return VectorMath.AverageVector(new Vector3[4] { cpUL, cpUR, cpDL, cpDR });
    }


    // Add a point to the set of points belonging to the plane
    private void addPoint(Vector3 point) {
      planePoints.Add(new Vector2(point.X, point.Y));
    }

    // Get the depth of a point given its (x, y) coordinate
    private ushort getDepth(int x, int y) {
      return depthData[pointHash(x, y)];
    }

    // Get the 3D Vector with first two components x and y
    private Vector3 getVector(int x, int y) {
      return new Vector3(x, y, getDepth(x, y));
    }

    // Format the raw data to
    //   a. Mirror the feed horizontally
    //   b. Mask the bottom 3 bits to get the raw millimeter distance
    private ushort[] formatShortData(short[] raw) {
      ushort[] shorts = new ushort[raw.Length];
      for (int i = 0; i < raw.Length; i++) {
        int x = i % WIDTH;
        int rest = i - x;
        shorts[rest + (WIDTH - x - 1)] = (ushort)(((ushort)raw[i]) >> 3);
      }
      return shorts;
    }

    // When a video frame is ready
    private void OnColorFrameReady(object sender, ColorImageFrameReadyEventArgs e) {
      if (colorUpdateReady) {
        using (ColorImageFrame imageFrame = e.OpenColorImageFrame()) {
          if (imageFrame != null) {
            // Drop updates if they start to pile up
            colorUpdateReady = false;

            Monitor.Enter(imageLock);

            byte[] imageData = new byte[imageFrame.PixelDataLength];
            imageFrame.CopyPixelDataTo(imageData);

            XnaImageData = new uint[imageData.Length / 4];
            for (int i = 0; i < XnaImageData.Length; i++) {
              int x = i % WIDTH;
              int rest = i - x;

              byte r = imageData[(i * 4)];
              byte g = imageData[(i * 4) + 1];
              byte b = imageData[(i * 4) + 2];

              uint col = 0xFF;
              col = (col << 8) + r;
              col = (col << 8) + g;
              col = (col << 8) + b;

              XnaImageData[rest + (WIDTH - x - 1)] = col;
            }

            colorUpdateReady = true;

            Monitor.Exit(imageLock);
          }
        }
      }
    }



    // When a skeleton frame is ready
    public void OnSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e) {
      if (skeletonUpdateReady) {
        using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame()) {
          if (skeletonFrame != null) {
            skeletonUpdateReady = false;
            Monitor.Enter(skeletonLock);

            if (jointPoints != null) {
              jointPoints.Clear();
            } else {
              jointPoints = new List<Vector2>();
            }

            if (joints != null) {
              joints.Clear();
            } else {
              joints = new List<Joint>();
            }

            Skeleton[] data = new Skeleton[skeletonFrame.SkeletonArrayLength];
            skeletonFrame.CopySkeletonDataTo(data);

            Skeleton skeleton = null;
            if (data != null) {
              foreach (Skeleton skel in data) {
                if (skel.TrackingState == SkeletonTrackingState.Tracked) {
                  skeleton = skel;
                }
              }
            }

            if (skeleton != null) {
              CoordinateMapper mapper = new CoordinateMapper(sensor);
              foreach (Joint j in skeleton.Joints) {
                DepthImagePoint pt = mapper.MapSkeletonPointToDepthPoint(j.Position, DepthImageFormat.Resolution640x480Fps30);
                joints.Add(j);
                addSquare(pt.X, pt.Y);
              }
            }
            skeletonUpdateReady = true;

            Monitor.Exit(skeletonLock);
          }
        }
      }
    }

    // Add a 5 x 5 square centered on (x, y)
    private void addSquare(int x, int y) {
      for (int i = 0; i < 5; i++) {
        for (int j = 0; j < 5; j++) {
          tryAdd(x - 2 + i, y - 2 + j);
        }
      }
    }

    // If an (x, y) pair is valid, add it to the joint positions list
    private void tryAdd(int x, int y) {
      x = (WIDTH - x - 1);
      if (x >= 0 && x < WIDTH && y >= 0 && y < HEIGHT) {
        jointPoints.Add(new Vector2(x, y));
      }
    }

    // If the sensor is running
    public bool IsRunning {
      get {
        return sensor != null && sensor.IsRunning;
      }
    }

    // The current Xna Image
    public uint[] CurrentXnaImageData {
      get {
        uint[] data = null;

        Monitor.Enter(imageLock);
        if (XnaImageData != null) {
          data = (uint[])XnaImageData.Clone();
        }
        Monitor.Exit(imageLock);

        return data;
      }
    }

    public Vector3[] CurrentDepthPlanePoints {
      get {
        Vector3[] copyPoints = null;

        Monitor.Enter(depthLock);
        if (depthPlanePoints != null) {
          copyPoints = new Vector3[depthPlanePoints.Count];
          depthPlanePoints.CopyTo(copyPoints);
        }
        Monitor.Exit(depthLock);

        return copyPoints;
      }
    }

    // The current Set of points belonging to the plane
    public Vector2[] CurrentPlanePoints {
      get {
        Vector2[] copyPoints = null;

        Monitor.Enter(depthLock);
        if (planePoints != null) {
          copyPoints = new Vector2[planePoints.Count];
          planePoints.CopyTo(copyPoints);
        }
        Monitor.Exit(depthLock);

        return copyPoints;
      }
    }

    // The current plane
    public Plane CurrentPlane {
      get {
        Plane data = null;

        Monitor.Enter(depthLock);
        if (plane != null) {
          data = (Plane)plane.Clone();
        }
        Monitor.Exit(depthLock);

        return data;
      }
    }

    // The current positions of the joints
    public Vector2[] CurrentJointPoints {
      get {
        Vector2[] copyPoints = null;

        Monitor.Enter(skeletonLock);
        if (jointPoints != null) {
          copyPoints = new Vector2[jointPoints.Count];
          jointPoints.CopyTo(copyPoints, 0);
        }
        Monitor.Exit(skeletonLock);

        return copyPoints;
      }
    }

    // The current joints
    public Joint[] CurrentJoints {
      get {
        Joint[] copyJoints = null;

        Monitor.Enter(skeletonLock);
        if (joints == null) {
          copyJoints = new Joint[joints.Count];
          joints.CopyTo(copyJoints, 0);
        }
        Monitor.Exit(skeletonLock);

        return copyJoints;
      }
    }

    // The width of the feed
    public int Width {
      get {
        return WIDTH;
      }
    }

    // The height of the feed
    public int Height {
      get {
        return HEIGHT;
      }
    }

    // If the depth is enabled
    public bool DepthEnabled {
      get {
        return mode == KinectMode.ALL || mode == KinectMode.DEPTH
          || mode == KinectMode.DEPTH_AND_SKELETON || mode == KinectMode.DEPTH_AND_VIDEO;
      }
    }

    // If the video is enabled
    public bool VideoEnabled {
      get {
        return mode == KinectMode.ALL || mode == KinectMode.VIDEO
          || mode == KinectMode.VIDEO_AND_SKELETON || mode == KinectMode.DEPTH_AND_VIDEO;
      }
    }

    // If the skeleton is enabled
    public bool SkeletonEnabled {
      get {
        return mode == KinectMode.ALL || mode == KinectMode.SKELETON
          || mode == KinectMode.VIDEO_AND_SKELETON || mode == KinectMode.DEPTH_AND_SKELETON;
      }
    }
  }

  class ReturnTypeNotEnabledException { }

  enum KinectMode {
    VIDEO,
    DEPTH,
    SKELETON,
    DEPTH_AND_VIDEO,
    DEPTH_AND_SKELETON,
    VIDEO_AND_SKELETON,
    ALL
  }
}
