using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace KinectSample {
  /// <summary>
  /// Represents a mathematical plane
  /// </summary>
  class Plane : ICloneable {
    // Plane is represented by its normal and offset, D
    private Vector3 normal;
    private float d;

    private Plane() { }

    /// <summary>
    ///  Build a plane with Two Vectors v1, v2 and point on plane, p
    /// </summary>
    /// <param name="v1">A vector on the plane</param>
    /// <param name="v2">A vector on the plane</param>
    /// <param name="p">A point on the plane</param>
    public Plane(Vector3 v1, Vector3 v2, Vector3 p) {
      setPlane(v1, v2, p);
    }

    /// <summary>
    /// The normal of the plane
    /// </summary>
    public Vector3 Normal { get { return normal; } }

    /// <summary>
    /// The offset of the plane
    /// </summary>
    public float Offset { get { return d; } }

    /// <summary>
    /// Set the plane with Two Vectors v1, v2 and point on plane p
    /// </summary>
    /// <param name="v1">A vector on the plane</param>
    /// <param name="v2">A vector on the plane</param>
    /// <param name="p">A point on the plane</param>
    public void setPlane(Vector3 v1, Vector3 v2, Vector3 p) {
      normal = Vector3.Cross(v1, v2);
      d = -((normal.X * p.X) + (normal.Y * p.Y) + (normal.Z * p.Z));
    }

    /// <summary>
    /// Absolute distance of v from the plane
    /// </summary>
    /// <param name="v">A point</param>
    /// <returns>absolute value of the distance between the point and the plane</returns>
    public double getDistance(Vector3 v) {
      return Math.Abs(((normal.X * v.X) + (normal.Y * v.Y) + (normal.Z * v.Z) + d)
        / Vector3.Distance(normal, Vector3.Zero));
    }

    /// <summary>
    /// Clones the plane
    /// </summary>
    /// <returns>A clone of this plane</returns>
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
