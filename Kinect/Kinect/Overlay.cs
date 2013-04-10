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
    }
    /*
    private static float rotX = 0;
    private static float rotY = 0;
    */
    public KinectManager.Coordinate[] Rotate(Vector3 norm, Vector3 offset) {
      double rotY = Math.Atan2(norm.Z, norm.X) - Math.PI / 2;
      //rotX += .1f;
      double rotX = Math.Atan2(norm.Z, norm.Y) - Math.PI / 2;
      //rotX = 0;


      Vector3[] points = Points(col);
      KinectManager.Coordinate[] res = new KinectManager.Coordinate[points.Length];

      for (int i = 0; i < points.Length; i++) {
        Vector3 p = Rotate(points[i], rotX, rotY);
        res[i] = new KinectManager.Coordinate();
        res[i].color = col[i];
        res[i].point = new SkeletonPoint();
        res[i].point.X = p.X;
        res[i].point.Y = p.Y;
        res[i].point.Z = p.Z;
      }

      return res;
    }

    private Vector3 Rotate(Vector3 p, double angleX, double angleY) {
      Vector3 res = p;
      double cosX = Math.Cos(angleX);
      double sinX = Math.Sin(angleX);
      double cosY = Math.Cos(angleY);
      double sinY = Math.Sin(angleY);
      /*
      res.Y = (float)(cosX * res.Y - sinX * res.Z);
      res.Z = (float)(sinX * res.Y + cosX * res.Y);*/

      //res.X = (float)(cosY * res.X - sinY * res.Z);
      //res.Z = (float)(sinY * res.X - cosY * res.Z);

      //THANKS TO KEVIN WE HAVE THE MAGIC FORMULA!!!   :O
      //theta = Y
      // trident = Z
      // phi = X
      //sin phi = 0
      //cos phi = 1
      res.X = (float)(cosY * p.X + p.Y*sinX*sinY - p.Z *(sinY * cosX));
      res.Y = (float)(p.Y * cosX + p.Z * sinX);
      res.Z = (float)(sinY * p.X + -sinX * cosY * p.Y + cosY * cosX * p.Z);
        
      return res;
    }

    /*
    public KinectManager.Coordinate[] Rotate(Vector3 norm, Vector3 offset) {
      Vector3 z = new Vector3(0, 0, -1);
      double angle = Math.Acos(Vector3.Dot(norm, z) / norm.Length());

      double cos = Math.Cos(angle);
      double sin = Math.Sin(angle);

      Vector3 rot = Vector3.Normalize(Vector3.Cross(z, norm));
      if (cos == 1) {
        rot = Vector3.Up;
      }
      
      KinectManager.Coordinate[] res = new KinectManager.Coordinate[col.Length];

      Vector3[] points = Points(col);
      for (int i = 0; i < points.Length; i++) {
        Vector3 p = Rotate(points[i], rot, cos, sin);
        SkeletonPoint skPt = new SkeletonPoint();
          skPt.X = p.X;
          skPt.Y = p.Y;
          skPt.Z = p.Z;


        res[i] = new KinectManager.Coordinate();
        res[i].point = skPt;
        res[i].color = col[i];
      }

      return res;
    }

    private Vector3 Rotate(Vector3 point, Vector3 axis, double cos, double sin) {

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
    }*/

    private Vector3[] Points(Color[] col) {
      Vector3[] res = new Vector3[col.Length];
      for (int y = 0; y < height; y++) {
        for (int x = 0; x < width; x++) {
          res[y * width + x] = new Vector3(x - (width / 2), y - (height / 2), 0);
        }
      }
      return res;
    }

  }
}
