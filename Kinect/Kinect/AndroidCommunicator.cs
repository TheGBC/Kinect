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

    public float XAngle {
      get {
        string res = Orientation;
        if (res.Length > 0) {
          string[] parts = res.Split(':');
          Vector3 norm = new Vector3(
              float.Parse(parts[0]), 
              float.Parse(parts[1]), 
              float.Parse(parts[2]));
          return (float)Math.Atan2(norm.Z, norm.X);
        }
        return 0;
      }
    }

    public float YAngle {
      get {
        string res = Orientation;
        if (res.Length > 0) {
          string[] parts = res.Split(':');
          Vector3 norm = new Vector3(
              float.Parse(parts[0]),
              float.Parse(parts[1]),
              float.Parse(parts[2]));
          return (float)Math.Atan2(norm.Z, norm.Y);
        }
        return 0;
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

        using (StreamWriter writer = new StreamWriter(clientStream, Encoding.UTF8)) {
          writer.AutoFlush = true;
          writer.WriteLine("get-location");
        }

        using (StreamReader reader = new StreamReader(clientStream, Encoding.UTF8)) {
          return reader.ReadLine();
        }
      }
    }
  }
}
