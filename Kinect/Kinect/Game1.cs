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

    // Draws the 3d objects in multiple calls because the number of items to draw is too
    // large for one call
    //
    // 4 batches should be good enough, smallest unit is 3 because each triangle
    // needs to be drawn in the same batch
    private BatchHandler<VertexPositionColor> batchHandler = 
        new BatchHandler<VertexPositionColor>(4, 3);

    // 3D stuff
    private Overlay overlay;
    private Texture2D tex;

    private Arrow arrow;
    private Handler3D handler3D = new Handler3D();
    private List<VertexPositionColor> verts = new List<VertexPositionColor>();
    private List<Cube> cubes = new List<Cube>();

    // Kinect Manager to process depth and video
    private KinectManager manager = new KinectManager();

    // Communicates with Android
    private AndroidCommunicator androidBridge = AndroidCommunicator.Instance;

    // Texture to draw the color image to
    private Texture2D texture = null;

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

      tex = Content.Load<Texture2D>("g+");
      overlay = new Overlay(tex);

      handler3D.init(GraphicsDevice, Content.Load<Effect>("effects"));
      texture = new Texture2D(GraphicsDevice, manager.Width, manager.Height);
    }

    protected override void UnloadContent() { }

    protected override void Update(GameTime gameTime) {
      if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        this.Exit();
      base.Update(gameTime);
    }

    // Drawing Logic
    protected override void Draw(GameTime gameTime) {
      // Clear the screen
      GraphicsDevice.Clear(Color.CornflowerBlue);

      draw2d();

      base.Draw(gameTime);
    }

    private uint transparency(uint color, uint overlay) {
      float alpha = .25f;
      uint oldR = (color >> 16) & 0xFF;
      uint oldG = (color >> 8) & 0xFF;
      uint oldB = color & 0xFF;
      uint newR = (overlay >> 16) & 0xFF;
      uint newG = (overlay >> 8) & 0xFF;
      uint newB = overlay & 0xFF;

      uint r = (uint)(oldR * (1 - alpha) + newR * alpha);
      uint g = (uint)(oldG * (1 - alpha) + newG * alpha);
      uint b = (uint)(oldB * (1 - alpha) + newB * alpha);

      uint col = 0xFF;
      col = (col << 8) + r;
      col = (col << 8) + b;
      col = (col << 8) + b;

      return col;
    }

    private uint UintFromColor(Color color) {
      uint res = 255;
      res = (res << 8) + color.B;
      res = (res << 8) + color.G;
      res = (res << 8) + color.R;
      return res;
    }

    private KinectManager.Coordinate[] res = null;

    private void draw2d() {
      GraphicsDevice.Textures[0] = null;
      uint[] image = manager.Image;
      var coords = manager.Frame;
      

      if (image == null || coords == null || coords.Length < 3) {
        return;
      }
      
      Plane plane = manager.Plane;

      bool[] planePoints = manager.PlanePoints;

      if (plane != null /*&& planePoints != null*/) {
        /*
        foreach (var coord in coords) {
          Vector3 v = new Vector3(coord.point.X, coord.point.Y, coord.point.Z);
          if (plane.getDistance(v) < .1) {
            ColorImagePoint col = manager.Map(coord.point);
            if (col.X >= 0 && col.X < manager.Width && col.Y >= 0 && col.Y < manager.Height) {
              image[col.Y * manager.Width + col.X] = transparency(image[col.Y * manager.Width + col.X], 0xFFFF0000);

              SkeletonPoint pt = new SkeletonPoint();
              pt.X = col.X;
              pt.Y = col.Y;
              pt.Z = 0;

              planePoints.Add(pt);
            }
          }
        }*/
        
        res = overlay.Rotate(plane.Normal, plane.Point);
        
        uint[] imgOverlay = new uint[manager.Width * manager.Height];

        foreach (var pt in res) {

          SkeletonPoint point = pt.point;

          //Debug.WriteLine(point.Y + " " + point.X);
          //Debug.WriteLine(point.Z);
          int pX = (int)((Math.Floor((overlay.Width * (point.X / point.Z))) + (manager.Width / 2)));
          int pY = (int)((Math.Floor((overlay.Height * (point.Y / point.Z))) + ((manager.Height / 2))));

          

          int ind = (int)(pY * manager.Width + pX);
          if (ind >= 0 && ind < manager.Width * manager.Height) {
            imgOverlay[ind] = UintFromColor(pt.color);
          }
        }

        imgOverlay = Algorithm.Dilation(imgOverlay, manager.Width, manager.Height);
        for (int i = 0; i < imgOverlay.Length; i++) {
          if (imgOverlay[i] != 0 && planePoints[i]) {
            image[i] = transparency(image[i], imgOverlay[i]);
          }
        }
      }

      texture.SetData<uint>(image);

      spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
      spriteBatch.Draw(texture, new Rectangle(0, 0, manager.Width, manager.Height), Color.White);
      spriteBatch.End();
    }

    private bool contains(List<ColorImagePoint> points, int x, int y) {
      foreach (ColorImagePoint point in points) {
        if (point.X == x && point.Y == y) {
          return true;
        }
      }
      return false;
    }

    private void draw3d(int res) {
      var depth = manager.Frame;

      // Only displays 1/res of the points of the point cloud
      if (depth != null && depth.Length > 0) {

        // Run RANSAC with the center of the image being part of the plane
        var target = depth[depth.Length / 2];
        depth[depth.Length / 2] = depth[0];
        depth[0] = target;

        //Plane plane = manager.RANSAC(target, depth);

        cubes.Clear();
        verts.Clear();

        int ind = 0;
        foreach (var point in depth) {
          if (++ind == res) {
            ind = 0;

            // Points on the plane are colored red, else their own color
            Vector3 v = new Vector3(point.point.X, point.point.Y, point.point.Z);
            //if (plane.getDistance(v) < .1) {
            //  cubes.Add(new Cube(point.point, Color.Red));
            //} else {
              
              cubes.Add(new Cube(point.point, point.color));
            //}
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




