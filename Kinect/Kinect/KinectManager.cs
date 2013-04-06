using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;

namespace KinectSample {
  class KinectManager {
    private KinectSensor sensor = null;

    private bool frameReady = true;
    private object frameLock = new object();

    private readonly int WIDTH = 640;
    private readonly int HEIGHT = 480;

    private List<Coordinate> points = null;

    private readonly DepthImageFormat DEPTH_FORMAT = DepthImageFormat.Resolution320x240Fps30;
    private readonly ColorImageFormat COLOR_FORMAT = ColorImageFormat.RgbResolution640x480Fps30;

    private readonly int MIN_PLANE_MEMBERS = 1000;
    private readonly int MAX_ITERATION = 300;

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
    /// 
    /// </summary>
    /// <param name="coordinates"></param>
    /// <returns></returns>
    public Plane RANSAC(Coordinate[] coordinates) {
      Random r = new Random();
      Coordinate target = coordinates[r.Next(coordinates.Length)];
      coordinates[r.Next(coordinates.Length)] = coordinates[0];
      coordinates[0] = target;
      return RANSAC(target, coordinates);
    }

    /// <summary>
    /// Runs the RANSAC plane detection algorithm on the coordinates give
    /// </summary>
    /// <param name="coordinates">coordinates to run RANSAC on</param>
    /// <returns>A plane that fits at least 100 points in the list</returns>
    public Plane RANSAC(Coordinate target, Coordinate[] coordinates) {
      Plane plane = null;

      int planeCnt = 0;
      int iteration = 0;

      while (planeCnt < MIN_PLANE_MEMBERS && ++iteration < MAX_ITERATION) {
        SkeletonPoint targetSkeleton = target.point;
        Vector3[] points = new Vector3[3];
        points[0] = new Vector3(targetSkeleton.X, targetSkeleton.Y, targetSkeleton.Z);

        Random rand = new Random();

        for (int i = 1; i < 3; i++) {
          int randInd = rand.Next(coordinates.Length - i) + i;
          Coordinate coord = coordinates[randInd];

          points[i] = new Vector3(coord.point.X, coord.point.Y, coord.point.Z);

          coordinates[randInd] = coordinates[i];
          coordinates[i] = coord;
        }

        Vector3 v1 = Vector3.Subtract(points[0], points[1]);
        Vector3 v2 = Vector3.Subtract(points[0], points[2]);
        Plane nextPlane = new Plane(v1, v2, points[0]);


        int cnt = 0;
        foreach (Coordinate c in coordinates) {
          Vector3 v = new Vector3(c.point.X, c.point.Y, c.point.Z);
          if (nextPlane.getDistance(v) < .001) {
            cnt++;
          }
        }

        if (cnt > planeCnt) {
          planeCnt = cnt;
          plane = nextPlane;
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
          CoordinateMapper mapper = new CoordinateMapper(sensor);
          DepthImagePixel[] depthPixels = new DepthImagePixel[depthFrame.PixelDataLength];
          SkeletonPoint[] realPoints = new SkeletonPoint[depthFrame.PixelDataLength];
          byte[] colorData = new byte[colorFrame.PixelDataLength];

          // Clear the coordinates from the previous frame
          if (points != null) {
            points.Clear();
          } else {
            points = new List<Coordinate>();
          }

          // Obtain raw data from frames
          depthFrame.CopyDepthImagePixelDataTo(depthPixels);
          colorFrame.CopyPixelDataTo(colorData);

          // Map depth to real world skeleton points
          mapper.MapDepthFrameToSkeletonFrame(DEPTH_FORMAT, depthPixels, realPoints);

          // Select the points that are within range and add them to coordinates
          for (int i = 0; i < realPoints.Length; i++) {
            if (depthPixels[i].Depth >= depthFrame.MaxDepth
                || depthPixels[i].Depth <= depthFrame.MinDepth) {
                  continue;
            }

            Coordinate coord = new Coordinate();
            ColorImagePoint colorPoint = 
                mapper.MapSkeletonPointToColorPoint(realPoints[i], COLOR_FORMAT);

            coord.point = realPoints[i];
            coord.color = ColorFromColorPoint(colorPoint, colorData);
            points.Add(coord);
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

    public struct Coordinate {
      public SkeletonPoint point;
      public Color color;
    }
  }
}
