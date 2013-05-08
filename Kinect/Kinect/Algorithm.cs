using KinectSample;
using Microsoft.Kinect;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KinectSample {
  class Algorithm {
    /// <summary>
    /// Runs the RANSAC plane detection algorithm on the coordinates given
    /// </summary>
    /// <param name="coordinates">coordinates to run RANSAC on</param>
    /// <returns>A plane that fits the required amount of points</returns>
    public static Plane Ransac(List<KinectManager.Coordinate> coordinates, HashSet<SkeletonPoint> planePoints) {

      // Get a random target
      Random r = new Random();
      KinectManager.Coordinate target = coordinates[r.Next(coordinates.Count)];
      coordinates[r.Next(coordinates.Count)] = coordinates[0];
      coordinates[0] = target;

      return Ransac(target, coordinates, planePoints);
    }

    /// <summary>
    /// Runs the RANSAC plane detection algorithm on the coordinates given with a 
    /// point, target, on the plane
    /// </summary>
    /// <param name="target">A coordinate on the plane</param>
    /// <param name="coordinates">coordinates to run RANSAC on, target should
    /// be the first element in coordinates</param>
    /// <returns>A plane that fits the required amount of points</returns>
    public static Plane Ransac(KinectManager.Coordinate target,
        List<KinectManager.Coordinate> coordinates,
        HashSet<SkeletonPoint> planePoints) {
      Plane plane = null;

      // The initial number of members in the current plane
      int memberCount = 0;
      int iterations = 0;

      // Loop until one condition is met
      while (memberCount < coordinates.Count / 5 && ++iterations < 100) {
        // Clear all members
        planePoints.Clear();

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
          KinectManager.Coordinate coord = coordinates[randInd];

          points.Add(new Vector3(coord.point.X, coord.point.Y, coord.point.Z));

          coordinates[randInd] = coordinates[i];
          coordinates[i] = coord;
        }


        // Build two vectors from the three points and then calculate the plane
        Vector3 v1 = Vector3.Subtract(points[0], points[1]);
        Vector3 v2 = Vector3.Subtract(points[0], points[2]);
        Plane nextPlane = new Plane(v1, v2, points[0]);


        // See how many points are on the plane
        int cnt = 0;
        foreach (KinectManager.Coordinate c in coordinates) {
          Vector3 v = new Vector3(c.point.X, c.point.Y, c.point.Z);

          double d = nextPlane.getDistance(v);
          if (d < .01) {
            cnt++;
          }

          if (d < .1) {
            planePoints.Add(c.point);
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

    /// <summary>
    /// Turns on all pixels which fit one of the three masks
    /// Mask 1 : The pixel is above and below pixels which are on
    /// Mask 2 : The pixel is to the left and right of pixels which are on
    /// Mask 3 : The pixel is surrounded on all four corners by pixels which are on
    /// </summary>
    /// <param name="input">pixel array</param>
    /// <param name="width">width of pixel array</param>
    /// <param name="height">height of pixel array</param>
    /// <returns>new pixel array</returns>
    public static bool[] Dilation(bool[] input, int width, int height) {
      for (int y = 1; y < height - 1; y++) {
        for (int x = 1; x < width - 1; x++) {
          int up_left    = (y - 1) * width + (x - 1);
          int up         = (y - 1) * width + (  x  );
          int up_right   = (y - 1) * width + (x + 1);
          int left       = (  y  ) * width + (x - 1);
          int right      = (  y  ) * width + (x + 1);
          int down_left  = (y + 1) * width + (x - 1);
          int down       = (y + 1) * width + (  x  );
          int down_right = (y + 1) * width + (x + 1);
          int ind        = (  y  ) * width + (  x  );

          if (!input[ind]) {
            // Up and Down
            if ((input[up] && input[down])
            // Left and Right
                || (input[left] && input[right])
            // All four corners
                || (input[up_left] && input[up_right] && input[down_left] && input[down_right])) {
              input[ind] = true;
            }
          }
        }
      }
      return input;
    }

    public static Vector3 transformPoint(Vector3 v, Matrix4 matrix) {
      Vector3 t = new Vector3(v.X, v.Y, v.Z);
      Vector3 newV = new Vector3();
      newV.X = matrix.M11 * t.X + matrix.M12 * t.Y + matrix.M13 * t.Z;
      newV.Y = matrix.M21 * t.X + matrix.M22 * t.Y + matrix.M23 * t.Z;
      newV.Z = matrix.M31 * t.X + matrix.M32 * t.Y + matrix.M33 * t.Z;
      

      /*
      newV.X = matrix.M11 * v.X + matrix.M21 * v.Y + matrix.M31 * v.Z;
      newV.Y = matrix.M12 * v.X + matrix.M22 * v.Y + matrix.M32 * v.Z;
      newV.Z = matrix.M13 * v.X + matrix.M23 * v.Y + matrix.M33 * v.Z;
      */

      //Debug.WriteLine(matrix.M14 + " " + matrix.M24 + " " + matrix.M34);

      return newV;
    }

    /// <summary>
    /// Same as other dilation, exception with colors
    /// </summary>
    /// <param name="input">pixel array</param>
    /// <param name="width">width of pixel array</param>
    /// <param name="height">height of pixel array</param>
    /// <returns>new pixel array</returns>
    public static uint[] Dilation(uint[] input, int width, int height) {
      for (int y = 1; y < height - 1; y++) {
        for (int x = 1; x < width - 1; x++) {
          uint up_left = input[(y - 1) * width + (x - 1)];
          uint up = input[(y - 1) * width + (x)];
          uint up_right = input[(y - 1) * width + (x + 1)];
          uint left = input[(y) * width + (x - 1)];
          uint right = input[(y) * width + (x + 1)];
          uint down_left = input[(y + 1) * width + (x - 1)];
          uint down = input[(y + 1) * width + (x)];
          uint down_right = input[(y + 1) * width + (x + 1)];
          uint ind = input[(y) * width + (x)];

          uint[] arr = { up_left, up, up_right, left, right, down_left, down, down_right };

          if (ind == 0) {
            // Up and Down
            if ((up != 0 && down != 0)
              // Left and Right
                || (left != 0 && right != 0)
              // All four corners
                || (up_left != 0 && up_right != 0 
                    && down_left != 0 && down_right != 0)) {
              uint cnt = 0;
              uint r = 0;
              uint g = 0;
              uint b = 0;
              foreach (uint col in arr) {
                if (col != 0) {
                  cnt++;
                  r += (col >> 16) & 0xFF;
                  g += (col >> 8 ) & 0xFF;
                  b += (col) & 0xFF;
                }
              }


              if (cnt != 0) {
                uint c = 0xFF;
                c = (c << 8) + (r / cnt);
                c = (c << 8) + (g / cnt);
                c = (c << 8) + (b / cnt);

                input[y * width + x] = c;
              }
            }
          }
        }
      }
      return input;
    }
  }
}
