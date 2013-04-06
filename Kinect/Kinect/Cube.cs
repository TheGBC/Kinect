using Microsoft.Kinect;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinect {

  /**
   * Cube object to be rendered
   */
  class Cube {
    // Keeps track of the 8 cube corners
    private Vector3[] verts = new Vector3[8];

    // Indices to draw the 12 triangles
    private VertexPositionColor[] points = new VertexPositionColor[36];

    // Color generated from the depth
    private Color color = Color.Black;

    // Construct a cube at center
    public Cube(SkeletonPoint center) {
      init(center);
    }

    public Cube(SkeletonPoint center, Color color){
      this.color = color;
      init(center);
    }

    // Get the indices of the cube
    public VertexPositionColor[] Points {
      get {
        return points;
      }
    }

    // Generate the cube given the center
    public void init(SkeletonPoint center) {

      float x_off = .005f;
      float y_off = .005f;
      float z_off = .005f;

      // Get Corners
      verts[0] = new Vector3(center.X + x_off, center.Y + y_off, center.Z - z_off);
      verts[1] = new Vector3(center.X - x_off, center.Y + y_off, center.Z - z_off);
      verts[2] = new Vector3(center.X + x_off, center.Y - y_off, center.Z - z_off);
      verts[3] = new Vector3(center.X - x_off, center.Y - y_off, center.Z - z_off);
      verts[4] = new Vector3(center.X + x_off, center.Y + y_off, center.Z + z_off);
      verts[5] = new Vector3(center.X - x_off, center.Y + y_off, center.Z + z_off);
      verts[6] = new Vector3(center.X + x_off, center.Y - y_off, center.Z + z_off);
      verts[7] = new Vector3(center.X - x_off, center.Y - y_off, center.Z + z_off);

      // Get Indices
      points = indexToVertices(new Vector3[] {
        // Front Face
        verts[2], verts[1], verts[0], verts[2], verts[3], verts[1],

        // Right Face
        verts[1], verts[3], verts[7], verts[7], verts[5], verts[1],

        // Top Face
        verts[3], verts[6], verts[7], verts[2], verts[6], verts[3],

        // Left Face
        verts[0], verts[4], verts[2], verts[6], verts[2], verts[4],

        // Back Face
        verts[7], verts[6], verts[4], verts[7], verts[4], verts[5],

        // Bottom Face
        verts[1], verts[5], verts[0], verts[4], verts[0], verts[5]
      });
    }

    // Builds the Vertices given the indices
    private VertexPositionColor[] indexToVertices(Vector3[] inds) {
      VertexPositionColor[] arr = new VertexPositionColor[inds.Length];
      for (int i = 0; i < arr.Length; i++) {
        arr[i] = new VertexPositionColor();
        arr[i].Position = inds[i];
        arr[i].Color = color;
      }

      return arr;
    }
  }
}
