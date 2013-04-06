/**
 *  ******************************************************************
 *  * How To Successfully Connect your Android Phone to this Program *
 *  ******************************************************************
 *  
 *    (1) Make sure you have the following:
 *        (a) The android debug bridge driver for you phone installed on your computer
 *        (b) The android debug bridge program "adb", which is installed with the Android SDK
 *        (c) The PhoneStats (Kinect-Android) app installed on your android device
 *        
 *    (2) To make sure that your driver is the correct one, navigate to the platform-tools folder
 *        in the Android SDK folder (most likely in Program Files (x86)) and run the command with
 *        your phone plugged into your computer via USB:
 *        
 *          adb devices
 *          
 *        If a device is listed, then your driver is working properly.
 * 
 *    (3) In the same folder, run the command:
 *        
 *          adb forward tcp:8080 tcp:8080
 *          
 *    (4) Start the PhoneStats app on your phone
 *    
 *    (5) Start the Kinect application
 * 
 *    (6) The arrow on screen should move according to the phone's orientation, if this isn't
 *        happening or an exception is thrown (the Kinect app saying connection refused by host)
 *        try completely stopping the PhoneStats app, unplugging and replugging the phone, and
 *        going through the entire process again.
 */

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
using Kinect;

namespace KinectSample {
  public class Game1 : Microsoft.Xna.Framework.Game {
    // Graphics Device and Objects used to render onscreen
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;

    private long millis = 0;

    // 4 batches should be good enough, smallest is 3 because each triangle
    // needs to be drawn in the same batch
    private BatchHandler<VertexPositionColor> batchHandler = 
        new BatchHandler<VertexPositionColor>(4, 3);

    // 3D stuff
    private Arrow arrow;
    private Handler3D handler3D = new Handler3D();
    private List<VertexPositionColor> verts = new List<VertexPositionColor>();
    private List<Cube> cubes = new List<Cube>();

    // Kinect Manager to process depth and video
    private KinectManager manager = new KinectManager();

    // Communicates with Android
    private AndroidCommunicator androidBridge = AndroidCommunicator.Instance;

    public Game1() {
      graphics = new GraphicsDeviceManager(this);
      Content.RootDirectory = "Content";
      graphics.PreferredBackBufferHeight = 480;
      graphics.PreferredBackBufferWidth = 640;
    }

    protected override void Initialize() {
      base.Initialize();

      // Start the manager
      manager.Start();
    }

    protected override void LoadContent() {
      spriteBatch = new SpriteBatch(GraphicsDevice);
      arrow = new Arrow(Content.Load<Model>("arrow"));
      handler3D.init(GraphicsDevice, Content.Load<Effect>("effects"));
      
    }

    protected override void UnloadContent() { }

    protected override void Update(GameTime gameTime) {
      if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        this.Exit();

      KeyboardState keyboard = Keyboard.GetState();

      millis += (long)gameTime.ElapsedGameTime.TotalMilliseconds;

      // Up and Down zoom in and out
      if (keyboard.IsKeyDown(Keys.W)) {
        handler3D.Zoom += .01f;
      } else if (keyboard.IsKeyDown(Keys.S)) {
        handler3D.Zoom -= .01f;
      }

      // Left and Right rotate around
      if (keyboard.IsKeyDown(Keys.Left)) {
        handler3D.RotationY += .01f;
      } else if (keyboard.IsKeyDown(Keys.Right)) {
        handler3D.RotationY -= .01f;
      }

      // Top and Bottom rotate around
      if (keyboard.IsKeyDown(Keys.Up)) {
        handler3D.RotationX += .01f;
      } else if (keyboard.IsKeyDown(Keys.Down)) {
        handler3D.RotationX -= .01f;
      }


      handler3D.SetUpCamera();
      base.Update(gameTime);
    }

    // point is a 3d point to be rotated around unit vector axis by angle
    // Pass in the cos of angle and sin of angle to save processing time
    private Vector3 transformPoint(Vector3 point, Vector3 axis, float cos, float sin) {
      float dot = Vector3.Dot(point, axis);
      float u = axis.X;
      float v = axis.Y;
      float w = axis.Z;
      float x = point.X;
      float y = point.Y;
      float z = point.Z;

      return new Vector3(
          (u * dot * (1 - cos)) + (point.X * cos) + (((-w * y) + (v * z)) * sin),
          (v * dot * (1 - cos)) + (point.Y * cos) + (((w * x) + (u * z)) * sin),
          (w * dot * (1 - cos)) + (point.Z * cos) + (((-v * x) + (u * y)) * sin)
      );
    }

    // Drawing Logic
    protected override void Draw(GameTime gameTime) {
      // Clear the screen
      GraphicsDevice.Clear(Color.CornflowerBlue);

      draw3d(4);

      base.Draw(gameTime);
    }

    private void draw3d(int res) {
      var depth = manager.Frame;

      
      // Only displays 1/res of the points of the point cloud
      if (depth != null) {

        // Run RANSAC with the center of the image being part of the plane
        var target = depth[depth.Length / 2];
        depth[depth.Length / 2] = depth[0];
        depth[0] = target;

        Plane plane = manager.RANSAC(target, depth);

        cubes.Clear();
        verts.Clear();

        int ind = 0;
        foreach (var point in depth) {
          if (++ind == res) {
            ind = 0;

            // Points on the plane are colored red, else their color
            Vector3 v = new Vector3(point.point.X, point.point.Y, point.point.Z);
            if (plane.getDistance(v) < .1) {
              cubes.Add(new Cube(point.point, Color.Red));
            } else {
              cubes.Add(new Cube(point.point, point.color));
            }
          }
        }

        // Get cubes
        foreach (Cube cube in cubes) {
          verts.AddRange(cube.Points);
        }

        // Split up into different batches, draw
        List<VertexPositionColor>[] arr = batchHandler.handleBatches(verts);

        foreach (List<VertexPositionColor> batch in arr) {
          if (batch.Count == 0) {
            continue;
          }
          GraphicsDevice.DrawUserPrimitives(
            PrimitiveType.TriangleList,
            batch.ToArray(),
            0,
            batch.Count / 3,
            VertexPositionColor.VertexDeclaration);
        }
      }
    }
  }
}




