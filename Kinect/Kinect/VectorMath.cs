using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace KinectSample {
  // Used to perform Vector operations
  class VectorMath {
    private VectorMath() { }

    // Dot product of vectors v1, v2
    public static double DotProduct(Vector3 v1, Vector3 v2) {
      return (v1.X * v2.X) + (v1.Y * v2.Y) + (v1.Z * v2.Z);
    }

    // Magnitude of vector v
    public static double Magnitude(Vector3 v) {
      return Math.Sqrt(DotProduct(v, v));
    }

    // Angle between vectors v1, v2
    public static double AngleBetween(Vector3 v1, Vector3 v2) {
      double dot = DotProduct(v1, v2);
      double mag = Magnitude(v1) * Magnitude(v2);
      return Math.Acos(dot / mag);
    }

    // Cross product of vectors v1, v2
    public static Vector3 CrossProduct(Vector3 v1, Vector3 v2) {
      Vector3 res = new Vector3();

      res.X = (v1.Y * v2.Z) - (v1.Z * v2.Y);
      res.Y = -(v1.X * v2.Z) + (v1.Z * v2.X);
      res.Z = (v1.X * v2.Y) - (v1.Y * v2.X);

      return res;
    }

    // Find the average between two vectors v1, v2 (midpoint of the two points they represent)
    public static Vector3 AverageVector(Vector3 v1, Vector3 v2) {
      return new Vector3((v1.X + v2.X) / 2, (v1.Y + v2.Y) / 2, (v1.Z + v2.Z) / 2);
    }

    // Find the average between all the vectors (midpoint of the points they represent)
    public static Vector3 AverageVector(Vector3[] v) {
      Vector3 res = new Vector3();

      foreach (Vector3 vect in v) {
        res.X += vect.X;
        res.Y += vect.Y;
        res.Z += vect.Z;
      }

      res.X /= v.Length;
      res.Y /= v.Length;
      res.Z /= v.Length;

      return res;
    }

    // Return the vector pointing from p1 to p2 or the difference between vectors p1 and p2
    public static Vector3 VectorFrom(Vector3 p1, Vector3 p2) {
      return new Vector3(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
    }

    // Return the negative vector
    public static Vector3 Negative(Vector3 v) {
      return new Vector3(-v.X, -v.Y, -v.Z);
    }

    // Add two vectors v1, v2 together
    public static Vector3 Add(Vector3 v1, Vector3 v2) {
      return new Vector3(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
    }

    // Subtract two vectors v1, v2
    public static Vector3 Subtract(Vector3 v1, Vector3 v2) {
      return Add(v1, Negative(v2));
    }
  }
}
