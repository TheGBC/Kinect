using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Kinect;
using System.Diagnostics;
using System.Threading;

namespace KinectSample {
  public class Game1 : Microsoft.Xna.Framework.Game {
    // Graphics Device and Objects used to render onscreen
    private Model arrow;
    private Vector3 Position = new Vector3(-2, -1f, 2);

    private float Zoom = 10.0f;
    private float RotationY = 0.0f;
    private float RotationX = 0.0f;
    private Matrix gameWorldRotation;

    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    private Texture2D canvas;

    // Kinect Manager to process depth and video
    private KinectManager manager = new KinectManager(KinectMode.DEPTH_AND_VIDEO);

    public Game1() {
      graphics = new GraphicsDeviceManager(this);
      Content.RootDirectory = "Content";
    }

    protected override void Initialize() {
      base.Initialize();

      // Start the manager and initialize the canvas
      manager.Start();
      canvas = new Texture2D(GraphicsDevice, manager.Width, manager.Height);
    }

    protected override void LoadContent() {
      spriteBatch = new SpriteBatch(GraphicsDevice);
      arrow = Content.Load<Model>("arrow");
    }

    protected override void UnloadContent() { }

    protected override void Update(GameTime gameTime) {
      if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        this.Exit();

      base.Update(gameTime);
    }

    private void DrawModel(Model m) {
      Matrix[] transforms = new Matrix[m.Bones.Count];
      float aspectRatio = graphics.GraphicsDevice.Viewport.AspectRatio;
      m.CopyAbsoluteBoneTransformsTo(transforms);
      Matrix projection =
          Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45.0f),
          aspectRatio, 1.0f, 10.0f);
      Matrix view = Matrix.CreateLookAt(new Vector3(0.0f, 2.0f, Zoom),
          Vector3.Zero, Vector3.Up);

      foreach (ModelMesh mesh in m.Meshes) {
        foreach (BasicEffect effect in mesh.Effects) {
          effect.EnableDefaultLighting();

          effect.View = view;
          effect.Projection = projection;
          effect.World = transforms[mesh.ParentBone.Index] * Matrix.CreateTranslation(Position);
          effect.EnableDefaultLighting();
        }
        mesh.Draw();
      }
    }

    // Drawing Logic
    protected override void Draw(GameTime gameTime) {
      // Clear the screen
      GraphicsDevice.Clear(Color.CornflowerBlue);
      // Clear the Canvas
      GraphicsDevice.Textures[0] = null;

      

      // Begin Drawing
      spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

      
      // Obtain all the "Relevant Points"
      Vector3[] points = manager.CurrentDepthPlanePoints;
      // Obtain the video feed
      uint[] colors = manager.CurrentXnaImageData;

      // If the points are ready to be drawn
      if (points != null && colors != null) {
        // Replace the pixels in the video feed that are relevant with the color red
        for (int i = 0; i < points.Length; i++) {
          colors[(int)((points[i].Y * manager.Width) + points[i].X)] = 0xFF0000FF;
        }

        // Set the canvas and draw onto screen
        canvas.SetData<uint>(colors);
        spriteBatch.Draw(canvas, new Rectangle(0, 0, manager.Width, manager.Height), Color.White);
      }

      

      // Finish drawing
      spriteBatch.End();
      DrawModel(arrow);
      base.Draw(gameTime);
    }
  }
}
