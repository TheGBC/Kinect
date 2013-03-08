using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Kinect {
  class Arrow {
    private Model arrow;
    private Vector3 Position = new Vector3(0, 0, 0);
    private Vector3 Rotation = new Vector3(0, 0, 0);

    private float zoom = 15.0f;

    private Matrix gameWorldRotation = Matrix.Identity;

    public Arrow(Model arrow) {
      this.arrow = arrow;
      updateRotation();
    }

    public float RotX { set { Rotation.X = value; updateRotation(); } }
    public float RotY { set { Rotation.Y = value; updateRotation(); } }
    public float RotZ { set { Rotation.Z = value; updateRotation(); } }

    public float PosX { set { Position.X = value; } }
    public float PosY { set { Position.Y = value; } }
    public float PosZ { set { Position.Z = value; } }
    public float Zoom { set { zoom = value; } }

    private void updateRotation() {
      gameWorldRotation = Matrix.CreateRotationX(Rotation.X)
          * Matrix.CreateRotationY(Rotation.Y)
          * Matrix.CreateRotationZ(Rotation.Z);
    }

    public void DrawModel(GraphicsDeviceManager graphics) {
      Matrix[] transforms = new Matrix[arrow.Bones.Count];
      float aspectRatio = graphics.GraphicsDevice.Viewport.AspectRatio;
      arrow.CopyAbsoluteBoneTransformsTo(transforms);
      Matrix projection =
          Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45.0f),
          aspectRatio, 1.0f, 30.0f);
      Matrix view = Matrix.CreateLookAt(new Vector3(0, 0.0f, zoom),
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
