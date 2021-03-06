﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Kinect;

namespace KinectSample {
  /// <summary>
  /// Represents a mathematical plane
  /// </summary>
  class Plane : ICloneable {
    // Plane is represented by its normal and offset, D
    private Vector3 normal;
    private float d;
    private Vector3 p;

    private Plane() { }

    /// <summary>
    ///  Build a plane with Two Vectors v1, v2 and point on plane, p
    /// </summary>
    /// <param name="v1">A vector on the plane</param>
    /// <param name="v2">A vector on the plane</param>
    /// <param name="p">A point on the plane</param>
    public Plane(Vector3 v1, Vector3 v2, Vector3 p) {
      setPlane(v1, v2, p);
      Matrix = Matrix4.Identity;
    }

    public Matrix4 Matrix { get; set; }

    public void Transform(Matrix4 mat) {
      normal = Algorithm.transformPoint(normal, mat);
      p = Algorithm.transformPoint(p, mat);
    }

    /// <summary>
    /// The normal of the plane
    /// </summary>
    public Vector3 Normal { 
      get {
        return Algorithm.transformPoint(normal, Matrix);
        //return normal;
      }
    }

    /// <summary>
    /// Point on Plane
    /// </summary>
    public Vector3 Point { 
      get {
        Vector3 v = Algorithm.transformPoint(p, Matrix);
        
        /*
        v.X -= Matrix.M41;
        v.Y -= Matrix.M42;
        v.Z -= Matrix.M43;
        */
        return v;
        //return Algorithm.transformPoint(p, Matrix);
        //return p;
      }
    }

    /// <summary>
    /// The offset of the plane
    /// </summary>
    public float Offset { 
      get {
        return -((Normal.X * Point.X) + (Normal.Y * Point.Y) + (Normal.Z * Point.Z)); 
      } 
    }

    /// <summary>
    /// Set the plane with Two Vectors v1, v2 and point on plane p
    /// </summary>
    /// <param name="v1">A vector on the plane</param>
    /// <param name="v2">A vector on the plane</param>
    /// <param name="p">A point on the plane</param>
    public void setPlane(Vector3 v1, Vector3 v2, Vector3 p) {
      Vector3 n1 = Vector3.Cross(v1, v2);
      Vector3 n2 = new Vector3(-n1.X, -n1.Y, -n1.Z);
      this.p = p;
      double d1 = Vector3.Add(n1, p).Length();
      double d2 = Vector3.Add(n2, p).Length();

      if (d1 > d2) {
        normal = n2;
      } else {
        normal = n1;
      }

      d = -((normal.X * p.X) + (normal.Y * p.Y) + (normal.Z * p.Z));
    }

    /// <summary>
    /// Absolute distance of v from the plane
    /// </summary>
    /// <param name="v">A point</param>
    /// <returns>absolute value of the distance between the point and the plane</returns>
    public double getDistance(Vector3 v) {
      Vector3 n = Normal;

      return Math.Abs(((n.X * v.X) + (n.Y * v.Y) + (n.Z * v.Z) + Offset)
        / n.Length());
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
      newPlane.p = p;

      newPlane.d = d;
      newPlane.Matrix = Matrix;
      return newPlane;
    }
  }
}
