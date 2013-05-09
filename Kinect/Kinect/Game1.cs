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

    // 3D stuff
    private Model3D graph;

    private Overlay overlay;
    private Texture2D tex;

    private List<VertexPositionColor> verts = new List<VertexPositionColor>();

    // Kinect Manager to process depth and video
    private KinectManager manager = new KinectManager();

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
      graph = new Model3D(Content.Load<Model>("graph"));
      tex = Content.Load<Texture2D>("recipe");
      overlay = new Overlay(tex);

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

      if (plane != null) {
        plane.Matrix = manager.Matrix;

        //graph.Rotate(plane.Normal, manager.AccelerometerReading());
        /*
        foreach (var coord in coords) {
          Vector3 v = new Vector3(coord.point.X, coord.point.Y, coord.point.Z);
          if (plane.getDistance(v) < .1) {
            ColorImagePoint col = manager.Map(coord.point);
            if (col.X >= 0 && col.X < manager.Width && col.Y >= 0 && col.Y < manager.Height) {
              image[col.Y * manager.Width + col.X] = 0xFFFF0000;
              if (col.X > 0) {
                uint c = image[col.Y * manager.Width + (col.X - 1)];
                image[col.Y * manager.Width + (col.X - 1)] = 0xFFFF0000;
              }
              if (col.X < manager.Width) {
                uint c = image[col.Y * manager.Width + (col.X + 1)];
                image[col.Y * manager.Width + (col.X + 1)] = 0xFFFF0000;
              }
              if (col.Y > 0) {
                uint c = image[(col.Y - 1) * manager.Width + col.X];
                image[(col.Y - 1) * manager.Width + col.X] = 0xFFFF0000;
              }
              if (col.Y < manager.Height) {
                uint c = image[(col.Y + 1) * manager.Width + col.X];
                image[(col.Y + 1) * manager.Width + col.X] = 0xFFFF0000;
              }
              if (col.X > 0 && col.Y > 0) {
                uint c = image[(col.Y - 1) * manager.Width + (col.X - 1)];
                image[(col.Y - 1) * manager.Width + (col.X - 1)] = 0xFFFF0000;
              }
              if (col.X < manager.Width && col.Y > 0) {
                uint c = image[(col.Y - 1) * manager.Width + (col.X + 1)];
                image[(col.Y - 1) * manager.Width + (col.X + 1)] = 0xFFFF0000;
              }
              if (col.X > 0 && col.Y < manager.Height) {
                uint c = image[(col.Y + 1) * manager.Width + (col.X - 1)];
                image[(col.Y + 1) * manager.Width + (col.X - 1)] = 0xFFFF0000;
              }
              if (col.X < manager.Width && col.Y < manager.Height) {
                uint c = image[(col.Y + 1) * manager.Width + (col.X + 1)];
                image[(col.Y + 1) * manager.Width + (col.X + 1)] = 0xFFFF0000;
              }
            }
          }
        }*/

        
        res = overlay.Rotate(plane.Normal, plane.Point, manager.AccelerometerReading(), manager.Matrix);
        uint[] imgOverlay = new uint[manager.Width * manager.Height];
        int largestSide = overlay.LargestSide;


        SkeletonPoint imgCenter = res[(overlay.Height + 1) * overlay.Width / 2].point;
        Vector3 c = plane.Point;
        SkeletonPoint skpt = new SkeletonPoint();
        skpt.X = c.X;
        skpt.Y = c.Y;
        skpt.Z = c.Z;

        ColorImagePoint cip = manager.Map(skpt);
        Vector2 t = new Vector2(cip.X, cip.Y);
        Vector2 offset = new Vector2(
            (float)Math.Floor((largestSide * (imgCenter.X / ((imgCenter.Z + 1))))),
            (float)Math.Floor((largestSide * (-imgCenter.Y / ((imgCenter.Z + 1))))));
        
        foreach (var pt in res) {
          SkeletonPoint point = pt.point;

          int pX = (int)((largestSide * (point.X / (point.Z + 1))) + t.X - offset.X);
          int pY = (int)((largestSide * (-point.Y / (point.Z + 1))) + t.Y - offset.Y);

          int ind = (int)(pY * manager.Width + pX);
          if (pX >= 0 && pX < manager.Width && pY >= 0 && pY < manager.Height) {
            imgOverlay[ind] = UintFromColor(pt.color);
          }
        }
        
        imgOverlay = Algorithm.Dilation(imgOverlay, manager.Width, manager.Height);
        for (int i = 0; i < imgOverlay.Length; i++) {
          if (imgOverlay[i] != 0 /*&& planePoints[i]*/) {
            image[i] = imgOverlay[i];
          }
        }
        
        texture.SetData<uint>(image);

        spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
        spriteBatch.Draw(texture, new Rectangle(0, 0, manager.Width, manager.Height), Color.White);
        spriteBatch.End();

      }
    }
    private bool contains(List<ColorImagePoint> points, int x, int y) {
      foreach (ColorImagePoint point in points) {
        if (point.X == x && point.Y == y) {
          return true;
        }
      }
      return false;
    }
  }
}




