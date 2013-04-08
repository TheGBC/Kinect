using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;

namespace KinectSample {
  /// <summary>
  /// Handles Processing with Kinect
  /// </summary>
  class KinectManager {

    // The Kinect Sensor
    private KinectSensor sensor = null;
    private CoordinateMapper mapper = null;

    // Locks to prevent untimely access to resources
    private bool frameReady = true;
    private object frameLock = new object();

    // Width and Height of frame
    private readonly int WIDTH = 640;
    private readonly int HEIGHT = 480;

    // Current Frame
    private List<Coordinate> points = null;
    private uint[] image = null;
    private DepthImagePixel[] depth = null;
    private Plane plane = null;

    // Formats for Depth and Color
    private readonly DepthImageFormat DEPTH_FORMAT = DepthImageFormat.Resolution640x480Fps30;
    private readonly ColorImageFormat COLOR_FORMAT = ColorImageFormat.RgbResolution640x480Fps30;

    // Initialize the KinectManager
    public KinectManager() {
      foreach (KinectSensor potentialSensor in KinectSensor.KinectSensors) {
        if (potentialSensor.Status == KinectStatus.Connected) {
          sensor = potentialSensor;
          break;
        }
      }

      if (sensor == null) {
        return;
      }

      mapper = new CoordinateMapper(sensor);

      sensor.DepthStream.Enable(DEPTH_FORMAT);
      sensor.ColorStream.Enable(COLOR_FORMAT);

      sensor.AllFramesReady += onFrameReady;
    }

    /// <summary>
    /// Start the sensor measurements
    /// </summary>
    public void Start() {
      sensor.Start();
    }

    /// <summary>
    /// If the sensor is running
    /// </summary>
    public bool IsRunning {
      get {
        return sensor != null && sensor.IsRunning;
      }
    }

    /// <summary>
    /// Width of Feed
    /// </summary>
    public int Width {
      get {
        return WIDTH;
      }
    }

    /// <summary>
    /// Height of Feed
    /// </summary>
    public int Height {
      get {
        return HEIGHT;
      }
    }

    /// <summary>
    /// The Current Frame
    /// </summary>
    public Coordinate[] Frame {
      get {
        Coordinate[] res = null;
        Monitor.Enter(frameLock);

        if (points != null) {
          res = new Coordinate[points.Count];
          points.CopyTo(res, 0);
        }

        Monitor.Exit(frameLock);
        return res;
      }
    }

    /// <summary>
    /// The Current Image
    /// </summary>
    public uint[] Image {
      get {
        uint[] res = null;
        Monitor.Enter(frameLock);

        if (image != null) {
          res = new uint[image.Length];
          image.CopyTo(res, 0);
        }

        Monitor.Exit(frameLock);
        return res;
      }
    }

    /// <summary>
    /// The Current Depth Image
    /// </summary>
    public DepthImagePixel[] Depth {
      get {
        DepthImagePixel[] res = null;
        Monitor.Enter(frameLock);

        if (depth != null) {
          res = new DepthImagePixel[depth.Length];
          depth.CopyTo(res, 0);
        }

        Monitor.Exit(frameLock);
        return res;
      }
    }

    public ColorImagePoint Map(SkeletonPoint pt) {
      return mapper.MapSkeletonPointToColorPoint(pt, COLOR_FORMAT);
    }

    public Plane Plane {
      get {
        Plane res = null;
        Monitor.Enter(frameLock);

        if (plane != null) {
          res = (Plane)plane.Clone();
        }

        Monitor.Exit(frameLock);
        return res;
      }
    }

    /// <summary>
    /// Runs the RANSAC plane detection algorithm on the coordinates given
    /// </summary>
    /// <param name="coordinates">coordinates to run RANSAC on</param>
    /// <returns>A plane that fits the required amount of points</returns>
    private Plane RANSAC(List<Coordinate> coordinates) {

      // Get a random target
      Random r = new Random();
      Coordinate target = coordinates[r.Next(coordinates.Count)];
      coordinates[r.Next(coordinates.Count)] = coordinates[0];
      coordinates[0] = target;

      return RANSAC(target, coordinates);
    }

    /// <summary>
    /// Runs the RANSAC plane detection algorithm on the coordinates given with a 
    /// point, target, on the plane
    /// </summary>
    /// <param name="target">A coordinate on the plane</param>
    /// <param name="coordinates">coordinates to run RANSAC on, target should
    /// be the first element in coordinates</param>
    /// <returns>A plane that fits the required amount of points</returns>
    private Plane RANSAC(Coordinate target, List<Coordinate> coordinates) {
      Plane plane = null;

      // The initial number of members in the current plane
      int memberCount = 0;
      int iterations = 0;

      // Loop until one condition is met
      while (memberCount < coordinates.Count / 15 || ++iterations > 100) {
        // Three points that fit on the plane
        List<Vector3> points = new List<Vector3>();

        // Get the first point from target
        SkeletonPoint targetSkeleton = target.point;
        points.Add(new Vector3(targetSkeleton.X, targetSkeleton.Y, targetSkeleton.Z));

        // Randomly find the next two points and swap them with the first few points in the list
        // (Like shuffling a deck)
        Random rand = new Random();
        for (int i = 1; i < 3; i++) {
          int randInd = rand.Next(coordinates.Count - i) + i;
          Coordinate coord = coordinates[randInd];

          points.Add(new Vector3(coord.point.X, coord.point.Y, coord.point.Z));

          coordinates[randInd] = coordinates[i];
          coordinates[i] = coord;
        }

        points.Sort((x, y) => {
          return (int)(1000 * (x.X - y.X));
        });

        points.Sort((x, y) => {
          return (int)(1000 * (x.Y - y.Y));
        });

        points.Sort((x, y) => {
          return (int)(1000 * (x.Z - y.Z));
        });
        

        // Build two vectors from the three points and then calculate the plane
        Vector3 v1 = Vector3.Subtract(points[0], points[1]);
        Vector3 v2 = Vector3.Subtract(points[0], points[2]);
        Plane nextPlane = new Plane(v1, v2, points[0]);

        // See how many points are on the plane
        int cnt = 0;
        foreach (Coordinate c in coordinates) {
          Vector3 v = new Vector3(c.point.X, c.point.Y, c.point.Z);
          double d = nextPlane.getDistance(v);
          if (d < .01) {
            cnt++;
          }
        }

        // If this plane has more members than the current one,
        // set the current plane to this plane
        if (cnt > memberCount) {
          plane = nextPlane;
          memberCount = cnt;
        }
      }
      return plane;
    }

    // When both color and depth frames are ready
    private void onFrameReady(object sender, AllFramesReadyEventArgs e) {

      // Only get the frames when we're done processing the previous one, to prevent
      // frame callbacks from piling up
      if (frameReady) {

        // Enter a context with both the depth and color frames
        using (DepthImageFrame depthFrame = e.OpenDepthImageFrame()) 
        using (ColorImageFrame colorFrame = e.OpenColorImageFrame()){

          // Lock resources and prevent further frame callbacks
          frameReady = false;
          Monitor.Enter(frameLock);

          // Init
          SkeletonPoint[] realPoints = new SkeletonPoint[depthFrame.PixelDataLength];
          depth = new DepthImagePixel[depthFrame.PixelDataLength];
          byte[] colorData = new byte[colorFrame.PixelDataLength];
          image = new uint[colorData.Length / 4];

          // Clear the coordinates from the previous frame
          if (points != null) {
            points.Clear();
          } else {
            points = new List<Coordinate>();
          }

          // Obtain raw data from frames
          depthFrame.CopyDepthImagePixelDataTo(depth);
          colorFrame.CopyPixelDataTo(colorData);

          // Build the image
          for (int i = 0; i < image.Length; i++) {
            byte r = colorData[(i * 4)];
            byte g = colorData[(i * 4) + 1];
            byte b = colorData[(i * 4) + 2];
            image[i] = ColorFromBytes(r, g, b);
          }

          // Map depth to real world skeleton points
          mapper.MapDepthFrameToSkeletonFrame(DEPTH_FORMAT, depth, realPoints);

          // Select the points that are within range and add them to coordinates
          for (int i = 0; i < realPoints.Length; i++) {
            if (depth[i].Depth >= depthFrame.MaxDepth
                || depth[i].Depth <= depthFrame.MinDepth) {
                  continue;
            }

            Coordinate coord = new Coordinate();
            ColorImagePoint colorPoint = 
                mapper.MapSkeletonPointToColorPoint(realPoints[i], COLOR_FORMAT);

            coord.point = realPoints[i];
            coord.color = ColorFromColorPoint(colorPoint, colorData);
            points.Add(coord);

            
          }

          if (depth.Length > 0 && depth[240 * 640 + 320].Depth > depthFrame.MinDepth && depth[240 * 640 + 320].Depth < depthFrame.MaxDepth) {
            Coordinate c = new Coordinate();
            c.point = realPoints[240 * 640 + 320];
            plane = RANSAC(c, points);
          }
          // Release resources, now ready for next callback
          Monitor.Exit(frameLock);
          frameReady = true;
        }
      }
    }

    // Get the color from a ColorImagePoint and the color data
    private Color ColorFromColorPoint(ColorImagePoint point, byte[] data){
      if (point.X < 0 || point.X >= WIDTH || point.Y < 0 || point.Y >= HEIGHT) {
        return Color.Purple;
      }

      int ind = (point.Y * WIDTH + point.X) * 4;

      byte r = data[ind];
      byte g = data[ind + 1];
      byte b = data[ind + 2];

      return new Color(r, g, b);
    }

    // Get the int color from rgb
    private uint ColorFromBytes(byte r, byte g, byte b) {
      uint res = 255;
      res = (res << 8) + r;
      res = (res << 8) + g;
      res = (res << 8) + b;
      return res;
    }

    /// <summary>
    /// A Coordinate that contains both position and color
    /// </summary>
    public struct Coordinate {
      public SkeletonPoint point;
      public Color color;
    }
  }
}
