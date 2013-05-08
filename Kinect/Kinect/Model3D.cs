using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Kinect {
  class Model3D {
    private Model model;
    private Vector3 Position = new Vector3(0, 0, 0);
    private Vector3 Rotation = new Vector3(0, 0, 0);

    private float zoom = 15.0f;

    private Matrix gameWorldRotation = Matrix.Identity;

    public Model3D(Model model) {
      this.model = model;
    }

    public float PosX { set { Position.X = value; } }
    public float PosY { set { Position.Y = value; } }
    public float PosZ { set { Position.Z = value; } }
    public float Zoom { set { zoom = value; } }

    private void updateRotation(float rotX, float rotY, float rotZ) {
      Rotation.X = rotX;
      Rotation.Y = rotY;
      //Rotation.Z = rotZ;
      gameWorldRotation = Matrix.CreateRotationX(Rotation.X) 
          * Matrix.CreateRotationY(Rotation.Y) 
          * Matrix.CreateRotationZ(Rotation.Z);
    }

    public void Rotate(Vector3 norm, Microsoft.Kinect.Vector4 orientation) {
      double rotY = -Math.Atan2(norm.X, norm.Z);
      double rotX = -Math.Atan2(norm.Y, norm.Z);
      double rotZ = Math.Atan2(orientation.Y, orientation.X) + Math.PI / 2;
      updateRotation((float)rotX, (float)rotY, (float)rotZ);
    }

    public void DrawModel(GraphicsDeviceManager graphics, Vector3 modelCenter) {
      Position = modelCenter;
      Matrix[] transforms = new Matrix[model.Bones.Count];
      float aspectRatio = graphics.GraphicsDevice.Viewport.AspectRatio;
      model.CopyAbsoluteBoneTransformsTo(transforms);
      Matrix projection =
          Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45.0f),
          aspectRatio, 1.0f, 30.0f);
      Matrix view = Matrix.CreateLookAt(new Vector3(0, 0.0f, zoom),
          Vector3.Zero, Vector3.Up);

      foreach (ModelMesh mesh in model.Meshes) {
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
