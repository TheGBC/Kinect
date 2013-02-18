using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kinect {
  class Arrow {
    private Model arrow;
    private Vector3 Position = new Vector3(1, -1f, 2);

    private float zoom = 15.0f;
    private float RotationY = 0.0f;
    private float RotationX = 0.0f;
    private float RotationZ = (float)(-Math.PI / 2);

    private Matrix gameWorldRotation = Matrix.Identity;

    public Arrow(Model arrow) {
      this.arrow = arrow;
      gameWorldRotation = Matrix.CreateRotationX(RotationX)
        * Matrix.CreateRotationY(RotationY)
        * Matrix.CreateRotationZ(RotationZ);
    }

    public float RotateX { set { RotationX = value; updateRotation(); } }
    public float RotateY { set { RotationY = value; updateRotation(); } }
    public float RotateZ { set { RotationZ = value; updateRotation(); } }
    public float PosX { set { Position.X = value; } }
    public float PosY { set { Position.Y = value; } }
    public float PosZ { set { Position.Z = value; } }
    public float Zoom { set { zoom = value; } }

    private void updateRotation() {
      gameWorldRotation = Matrix.CreateRotationX(RotationX)
        * Matrix.CreateRotationY(RotationY)
        * Matrix.CreateRotationZ(RotationZ);
    }

    public void DrawModel(GraphicsDeviceManager graphics) {
      Matrix[] transforms = new Matrix[arrow.Bones.Count];
      float aspectRatio = graphics.GraphicsDevice.Viewport.AspectRatio;
      arrow.CopyAbsoluteBoneTransformsTo(transforms);
      Matrix projection =
          Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45.0f),
          aspectRatio, 1.0f, 30.0f);
      Matrix view = Matrix.CreateLookAt(new Vector3(0.0f, 2.0f, zoom),
          Vector3.Zero, Vector3.Up);

      foreach (ModelMesh mesh in arrow.Meshes) {
        foreach (BasicEffect effect in mesh.Effects) {
          effect.EnableDefaultLighting();
          effect.View = view;
          effect.Projection = projection;
          effect.World = gameWorldRotation * transforms[mesh.ParentBone.Index] * Matrix.CreateTranslation(Position);
        }
        mesh.Draw();
      }
    }
  }
}
