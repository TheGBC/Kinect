using Microsoft.Kinect;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectSample {
  class Overlay {
    private Color[] col;
    private int width;
    private int height;
    
    public Overlay(Texture2D res) {
      width = res.Width;
      height = res.Height;
      col = new Color[res.Width * res.Height];
      res.GetData<Color>(col);
      
      for (int y = 0; y < res.Height / 2; y++) {
        for (int x = 0; x < res.Width; x++) {
          Color c = col[y * width + x];

          col[y * width + x] = col[(height - 1 - y) * width + x];
          col[(height - 1 - y) * width + x] = c;
        }
      }

    }
    /*
    //private static double rotX = 0;
    //private static double rotY = 0;
    public KinectManager.Coordinate[] Rotate(Vector3 norm, Vector3 offset) {
      double rotY = Math.Atan2(norm.X, norm.Z);
      double rotX = Math.Atan2(norm.Y, norm.Z);

      //rotY = Math.PI / 2;
      //rotX = 0; ;
      //rotY = 0;
      //rotX = 0;

      //Debug.WriteLine(MathHelper.ToDegrees((float)rotY));
      double cosX = Math.Cos(rotX);
      double sinX = Math.Sin(rotX);
      double cosY = Math.Cos(rotY);
      double sinY = Math.Sin(rotY);

      Vector3[] points = Points(col, offset);
      Vector3[] planePoints = new Vector3[3];

      KinectManager.Coordinate[] res = new KinectManager.Coordinate[points.Length];

      for (int i = 0; i < points.Length; i++) {
        Vector3 p = RotatePoint(points[i], cosX, sinX, cosY, sinY);
        res[i] = new KinectManager.Coordinate();
        res[i].color = col[i];
        res[i].point = new SkeletonPoint();
        res[i].point.X = p.X;
        res[i].point.Y = p.Y;
        res[i].point.Z = p.Z;
      }

      planePoints[0] = new Vector3(res[0].point.X, res[0].point.Y, res[0].point.Z);
      planePoints[1] = new Vector3(res[320].point.X, res[320].point.Y, res[320].point.Z);
      planePoints[2] = new Vector3(res[720].point.X, res[720].point.Y, res[720].point.Z);
      Plane plane = new Plane(Vector3.Subtract(planePoints[0], planePoints[1]), Vector3.Subtract(planePoints[0], planePoints[2]), planePoints[0]);


      float dot = Vector3.Dot(norm, plane.Normal);
      float mag = (norm.Length() * plane.Normal.Length());

      float ration = dot / mag;
      if (ration < -1) {
        ration = -1;
      } else if (ration > 1) {
        ration = 1;
      }

      float ang = (float)Math.Acos(ration);

      Debug.WriteLine(ang);

      return res;
    }

    private Vector3 RotatePoint(Vector3 p, double cosX, double sinX, double cosY, double sinY) {
      Vector3 res = new Vector3();
      //THANKS TO KEVIN WE HAVE THE MAGIC FORMULA!!!   :O
      //theta = Y
      // trident = Z
      // phi = X
      //sin phi = 0
      //cos phi = 1
      res.Y = (float)(p.Y * cosX + p.Z * sinX);
      res.X = (float)(cosY * p.X + p.Y * sinX * sinY - p.Z * (sinY * cosX));
      res.Z = (float)(sinY * p.X + -sinX * cosY * p.Y + cosY * cosX * p.Z);
        
      return res;
    }*/

    public int Width { get { return width; } }
    public int Height { get { return height; } }
    

    public KinectManager.Coordinate[] Rotate(Vector3 norm, Vector3 offset) {

      //norm.Z = Math.Abs(norm.Z); //postive z axis always
      Vector3 z = new Vector3(0, 0, -1);
      double angle = Math.Acos(Vector3.Dot(norm, z) / norm.Length());
      // Debug.WriteLine("Degrees:" + MathHelper.ToDegrees((float) angle));
      // Debug.WriteLine("z:" + z);
      // Debug.WriteLine("norm:" + norm);
      double sin = Math.Sin(angle);
      double cos = Math.Cos(angle);

      //Debug.WriteLine(MathHelper.ToDegrees((float)angle));

      Vector3 rot = Vector3.Cross(z, norm);
      if (rot.Length() > 0) {
        rot = Vector3.Normalize(rot);
      }

      //Debug.WriteLine(rot);

      //Debug.WriteLine("rotation vector:" + rot);
     

      //Debug.WriteLine(z + " " + norm);

      //Debug.WriteLine(rot);
      
      KinectManager.Coordinate[] res = new KinectManager.Coordinate[col.Length];
      Vector3[] planePoints = new Vector3[3];

      Vector3[] points = Points(col, offset);

      for (int i = 0; i < points.Length; i++) {
        Vector3 p = RotatePoint(points[i], rot, cos, sin);

        SkeletonPoint skPt = new SkeletonPoint();
          skPt.X = p.X;
          skPt.Y = p.Y;
          skPt.Z = p.Z;


        res[i] = new KinectManager.Coordinate();
        res[i].point = skPt;
        res[i].color = col[i];
      }

      
      planePoints[0] = new Vector3(res[0].point.X, res[0].point.Y, res[0].point.Z);
      planePoints[1] = new Vector3(res[320].point.X, res[320].point.Y, res[320].point.Z);
      planePoints[2] = new Vector3(res[720].point.X, res[720].point.Y, res[720].point.Z);
      Plane plane = new Plane(Vector3.Subtract(planePoints[0], planePoints[1]), Vector3.Subtract(planePoints[0], planePoints[2]), planePoints[0]);


      float dot = Vector3.Dot(norm, plane.Normal);
      float mag = (norm.Length() * plane.Normal.Length());

      float ration = dot / mag;
      if (ration < -1) {
        ration = -1;
      } else if (ration > 1) {
        ration = 1;
      }

      float ang = (float)Math.Acos(ration);
      


      //Debug.WriteLine(ang);

      return res;
    }

    private Vector3 RotatePoint(Vector3 point, Vector3 axis, double cos, double sin) {
      /*Quaternion q = new Quaternion(axis, (float)cos);
      Vector3 res = Vector3.Transform(point, q);
      return res;*/
      
      float x = point.X;
      float y = point.Y;
      float z = point.Z;
      float u = axis.X;
      float v = axis.Y;
      float w = axis.Z;

      float dot = Vector3.Dot(point, axis);

      float nX = (float)((u * dot * (1 - cos)) + (x * cos) + (sin * ((-w * y) + (v * z))));
      float nY = (float)((v * dot * (1 - cos)) + (y * cos) + (sin * ((w * x) - (u * z))));
      float nZ = (float)((w * dot * (1 - cos)) + (z * cos) + (sin * ((-v * x) + (u * y))));

      return new Vector3(nX, nY, nZ);
    }

    private Vector3[] Points(Color[] col, Vector3 v) {
      Vector3[] res = new Vector3[col.Length];
      for (int y = 0; y < height; y++) {
        for (int x = 0; x < width; x++) {
          float indX = (float)(x - width / 2) / (float)width;
          float indY = (float)(y - height / 2) / (float)height;
          //Debug.WriteLine(indX + " " + indY);
          //res[y * width + x] = new Vector3(x - (width / 2) , y - (height / 2), v.Z * 100);
          res[y * width + x] = new Vector3(indX, indY, v.Z);
        }
      }
      return res;
    }
    
  }
}
