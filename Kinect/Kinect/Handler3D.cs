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
   * Handles 3D View
   */
  class Handler3D {

    // Graphics Device to be used to get graphics information
    private GraphicsDevice device;

    // View, projection, and rotation matrix
    private Matrix viewMatrix;
    private Matrix projectionMatrix;
    private Matrix rotationMatrix;

    // Effect to use in rendering
    private Effect effect;

    // Zoom of camera
    public float Zoom { get; set; }

    // Y rotation of world (left/right)
    public float RotationY { get; set; }

    // X rotation of world (up/down)
    public float RotationX { get; set; }

    // Initialize the 3d handler
    public void init(GraphicsDevice device, Effect effect) {
      this.device = device;
      this.effect = effect;
      this.Zoom = 2;

      // Set the Technique for the Effect
      this.effect.CurrentTechnique = effect.Techniques["ColoredNoShading"];

      SetUpCamera();
    }

    // Update and apply the 3d view
    public void SetUpCamera() {
      viewMatrix = Matrix.CreateLookAt(new Vector3(0, 0, Zoom), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
      projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 1.0f, 1000.0f);
      rotationMatrix = Matrix.CreateRotationY(RotationY) * Matrix.CreateRotationX(RotationX);

      effect.Parameters["xView"].SetValue(viewMatrix);
      effect.Parameters["xProjection"].SetValue(projectionMatrix);
      effect.Parameters["xWorld"].SetValue(rotationMatrix);

      foreach (EffectPass pass in effect.CurrentTechnique.Passes) {
        pass.Apply();

      }
    }
  }
}
