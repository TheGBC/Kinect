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

    // 3D stuff
    private Arrow arrow;
    private Handler3D handler3D = new Handler3D();
    private List<VertexPositionColor> verts = new List<VertexPositionColor>();
    private List<Cube> cubes = new List<Cube>();


    // Kinect Manager to process depth and video
    private KinectManager manager = new KinectManager(KinectMode.DEPTH_AND_VIDEO);

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

      cubes.Add(new Cube(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight, 8192, new Vector3(0, 0, 0)));
      cubes.Add(new Cube(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight, 8192, new Vector3(0, 0, 8192)));
      cubes.Add(new Cube(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight, 8192, new Vector3(0, 480, 0)));
      cubes.Add(new Cube(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight, 8192, new Vector3(0, 480, 8192)));
      cubes.Add(new Cube(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight, 8192, new Vector3(640, 0, 0)));
      cubes.Add(new Cube(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight, 8192, new Vector3(640, 0, 8192)));
      cubes.Add(new Cube(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight, 8192, new Vector3(640, 480, 0)));
      cubes.Add(new Cube(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight, 8192, new Vector3(640, 480, 8192)));
      
    }

    protected override void UnloadContent() { }

    protected override void Update(GameTime gameTime) {
      if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        this.Exit();

      KeyboardState keyboard = Keyboard.GetState();

      // Up and Down zoom in and out
      if (keyboard.IsKeyDown(Keys.W)) {
        handler3D.Zoom -= .1f;
      } else if (keyboard.IsKeyDown(Keys.S)) {
        handler3D.Zoom += .1f;
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

    // Drawing Logic
    protected override void Draw(GameTime gameTime) {
      // Clear the screen
      GraphicsDevice.Clear(Color.CornflowerBlue);

      foreach (Cube cube in cubes) {
        verts.AddRange(cube.Points);
      }

      GraphicsDevice.DrawUserPrimitives(
          PrimitiveType.TriangleList,
          verts.ToArray(),
          0,
          verts.Count / 3,
          VertexPositionColor.VertexDeclaration);


      /*
      arrow.RotX = androidBridge.XAngle - (float)(Math.PI / 2);
      arrow.RotY = androidBridge.YAngle - (float)(Math.PI / 2);

      arrow.DrawModel(graphics);
      */
      base.Draw(gameTime);
    }
  }
}




