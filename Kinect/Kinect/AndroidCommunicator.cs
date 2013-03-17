using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Kinect {
  class AndroidCommunicator {
    private static AndroidCommunicator instance;
    private AndroidCommunicator() { }

    public static AndroidCommunicator Instance {
      get {
        if (instance == null) {
          instance = new AndroidCommunicator();
        }
        return instance;
      }
    }

    // Cross Product of phone normal and the up vector
    public Vector3 RotationAxis {
      get {
        Vector3 norm = Normal;
        if (norm.Equals(Vector3.Zero)) {
          return Vector3.Zero;
        }
        Vector3 up = Vector3.Up;

        Vector3 axis = Vector3.Cross(up, norm);
        axis.Normalize();
        return axis;
      }
    }

    // Angle between the phone normal and the up vector
    public float RotationAngle {
      get {
        Vector3 norm = Normal;
        if (norm.Equals(Vector3.Zero)) {
          return 0;
        }
        Vector3 up = Vector3.Up;

        return (float)Math.Acos(Vector3.Dot(norm, up) / (Vector3.Distance(norm, Vector3.Zero)));
      }
    }

    public float XAngle {
      get {
        Vector3 norm = Normal;
        if (norm.Equals(Vector3.Zero)) {
          return 0;
        }

        return (float)Math.Atan2(norm.Z, norm.X);
      }
    }

    public float YAngle {
      get {
        Vector3 norm = Normal;
        if (norm.Equals(Vector3.Zero)) {
          return 0;
        }

        return (float)Math.Atan2(norm.Z, norm.Y);
      }
    }

    private Vector3 Normal {
      get {
        string res = Orientation;
        if (res.Length > 0) {
          string[] parts = res.Split(':');
          Vector3 norm = new Vector3(
              float.Parse(parts[0]),
              float.Parse(parts[1]),
              float.Parse(parts[2]));
          return norm;
        }
        return Vector3.Zero;
      }
    }

    private string Orientation {
      get {
        TcpClient client = new TcpClient();
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);
        client.Connect(serverEndPoint);
        NetworkStream clientStream = client.GetStream();
        string str = "";

        StreamWriter writer = new StreamWriter(clientStream, Encoding.UTF8);
        writer.Write("get-orientation" + '\n');
        writer.Flush();

        StreamReader reader = new StreamReader(clientStream, Encoding.UTF8);
        str = reader.ReadLine();
        reader.Close();
        client.Close();
        return str;
      }
    }

    private string Location {
      get {
        TcpClient client = new TcpClient();
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080);
        client.Connect(serverEndPoint);
        NetworkStream clientStream = client.GetStream();
        string str = "";

        StreamWriter writer = new StreamWriter(clientStream, Encoding.UTF8);
        writer.Write("get-location" + '\n');
        writer.Flush();

        StreamReader reader = new StreamReader(clientStream, Encoding.UTF8);
        str = reader.ReadLine();
        reader.Close();
        client.Close();
        return str;
      }
    }
  }
}
