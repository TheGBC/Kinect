using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace KinectSample {
  // Object that represents a mathematical plane
  class Plane : ICloneable {
    // Plane is represented by its normal and offset, D
    private Vector3 normal;
    private float d;

    // Empty Plane
    private Plane() { }

    // Build a plane with Two Vectors v1, v2 and point on plane, p
    public Plane(Vector3 v1, Vector3 v2, Vector3 p) {
      setPlane(v1, v2, p);
    }

    // Get the plane Vector
    public Vector3 Normal { get { return normal; } }

    // Get the offset
    public float Offset { get { return d; } }

    // Set the plane with Two Vectors v1, v2 and point on plane p
    public void setPlane(Vector3 v1, Vector3 v2, Vector3 p) {
      normal = VectorMath.CrossProduct(v1, v2);
      d = -((normal.X * p.X) + (normal.Y * p.Y) + (normal.Z * p.Z));
    }

    // Get the distance of point v from the plane
    public double getDistance(Vector3 v) {
      return Math.Abs(((normal.X * v.X) + (normal.Y * v.Y) + (normal.Z * v.Z) + d))
        / VectorMath.Magnitude(normal);
    }

    // Get the point with x and y coordinates (finds the z)
    public Vector3 getPoint(float x, float y) {
      return new Vector3(x, y, -((normal.X * x) + (normal.Y * y) + d) / normal.Z);
    }

    // Clone the plane
    public object Clone() {
      Plane newPlane = new Plane();
      newPlane.normal = new Vector3();
      newPlane.normal.X = normal.X;
      newPlane.normal.Y = normal.Y;
      newPlane.normal.Z = normal.Z;

      newPlane.d = d;
      return newPlane;
    }
  }
}
