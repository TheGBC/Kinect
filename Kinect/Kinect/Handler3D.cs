using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinect {

  /// <summary>
  /// Handles 3D calculations and drawing functions
  /// </summary>
  class Handler3D {

    // Graphics Device to be used to get graphics information
    private GraphicsDevice device;

    // View, projection, and rotation matrix
    private Matrix viewMatrix;
    private Matrix projectionMatrix;
    private Matrix rotationMatrix;

    // Effect to use in rendering
    private Effect effect;

    /// <summary>
    /// Zoom of camera
    /// </summary>
    public float Zoom { get; set; }

    /// <summary>
    /// Y rotation (left/right)
    /// </summary>
    public float RotationY { get; set; }

    /// <summary>
    /// X rotation (up/down)
    /// </summary>
    public float RotationX { get; set; }

    /// <summary>
    /// Initialize the 3d handler
    /// </summary>
    /// <param name="device">The graphic device to use</param>
    /// <param name="effect">The effect to use</param>
    public void init(GraphicsDevice device, Effect effect) {
      this.device = device;
      this.effect = effect;
      this.Zoom = -2;

      // Set the Technique for the Effect
      this.effect.CurrentTechnique = effect.Techniques["ColoredNoShading"];

      SetUpCamera();
    }

    /// <summary>
    /// Update and apply the 3D values
    /// </summary>
    public void SetUpCamera() {
      viewMatrix = Matrix.CreateLookAt(new Vector3(0, 0, Zoom), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
      projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, device.Viewport.AspectRatio, 1.0f, 100.0f);
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
