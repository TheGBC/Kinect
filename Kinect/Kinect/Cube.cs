using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
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

    // Max Width, Height, and Depth of the input
    private float width;
    private float height;
    private float depth;

    // Color generated from the depth
    private Color color;

    // Construct an empty cube
    public Cube(float width, float height, float depth) {
      this.width = width;
      this.height = height;
      this.depth = depth;
    }

    // Construct a cube at center
    public Cube(float width, float height, float depth, Vector3 center) : this(width, height, depth){
      init(center);
    }

    // Get the indices of the cube
    public VertexPositionColor[] Points {
      get {
        return points;
      }
    }

    // Generate the cube given the center
    public void init(Vector3 center) {

      // Get color
      this.color = getColorFromDepth((center.Z / depth) - .5f);

      // Get depth
      float x_off = .01f * width;
      float y_off = .01f * height;
      float z_off = .01f * depth;

      // Get Corners
      verts[0] = transformVector(new Vector3(center.X - x_off, center.Y - y_off, center.Z + z_off));
      verts[1] = transformVector(new Vector3(center.X + x_off, center.Y - y_off, center.Z + z_off));
      verts[2] = transformVector(new Vector3(center.X - x_off, center.Y + y_off, center.Z + z_off));
      verts[3] = transformVector(new Vector3(center.X + x_off, center.Y + y_off, center.Z + z_off));
      verts[4] = transformVector(new Vector3(center.X - x_off, center.Y - y_off, center.Z - z_off));
      verts[5] = transformVector(new Vector3(center.X + x_off, center.Y - y_off, center.Z - z_off));
      verts[6] = transformVector(new Vector3(center.X - x_off, center.Y + y_off, center.Z - z_off));
      verts[7] = transformVector(new Vector3(center.X + x_off, center.Y + y_off, center.Z - z_off));

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

    // Generates the color based on depth
    // Assume depth is between -.5f and .5f
    private Color getColorFromDepth(float depth) {
      depth += depth + .5f;
      return new Color(depth, depth, depth);
    }

    // Normalizes a cooridnate to be in between -.5 and .5
    private Vector3 transformVector(Vector3 inVector) {
      return new Vector3((inVector.X / width) - .5f, (inVector.Y / height) - .5f, (inVector.Z / depth) - .5f);
    }
  }
}
