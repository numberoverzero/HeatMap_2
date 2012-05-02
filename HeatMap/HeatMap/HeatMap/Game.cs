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
using Engine;
using Engine.Utility;

namespace HeatMap
{
    static class Program{
        static void Main(string[] args){
            using (Game game = new Game())
                game.Run();
        }
    }

    public class Game : Microsoft.Xna.Framework.Game
    {
        
        GraphicsDeviceManager graphics;
        SpriteBatch batch;

        float panAmount = 4.0f; 
        Vector2 cameraPos; 
        Camera camera;
        Input input;

        int resolution = 7;
        int size;
        Map map;
        bool colored = true;
        Pen pen = new Pen(15, 0, 0.03f);
        Pen subPen = new Pen(15, 0, -0.03f);


        public Game()
        {
            IsMouseVisible = true;
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            input = new Input();

            // State
            input.AddKeyBinding("quit", Keys.Escape);
            
            // Camera
            input.AddKeyBinding("zoom_out", Keys.Q);
            input.AddKeyBinding("zoom_in", Keys.E);
            input.AddKeyBinding("pan_up", Keys.W);
            input.AddKeyBinding("pan_down", Keys.S);
            input.AddKeyBinding("pan_left", Keys.A);
            input.AddKeyBinding("pan_right", Keys.D);

            // ColorMaps
            input.AddKeyBinding("generate", Keys.Space);
            input.AddKeyBinding("cycle_map", Keys.Tab);
            input.AddKeyBinding("toggle_coloring", Keys.Space, Modifier.Shift);

            // Pen
            input.AddKeyBinding("pen_add", MouseButton.Left);
            input.AddKeyBinding("pen_sub", MouseButton.Right);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            batch = new SpriteBatch(GraphicsDevice);
            ShaderUtil.LoadContent(GraphicsDevice);
            camera = new Camera(GraphicsDevice.Viewport);
            size = (int)Math.Pow(2, resolution) + 1; 
            Map.LoadContent(Content, GraphicsDevice);
            map = new Map(size, size);
            map.AddColorMap(Map.DefaultColorMap);
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            camera.Update(gameTime.ElapsedGameTime.Milliseconds / 1000f); 
            input.Update();

            if (input.IsKeyBindingActive("quit"))
                this.Exit();
            if (input.IsKeyBindingActive("pan_up"))
                cameraPos.Y -= panAmount / camera.Scale.Y;
            if (input.IsKeyBindingActive("pan_down"))
                cameraPos.Y += panAmount / camera.Scale.Y;
            if (input.IsKeyBindingActive("pan_left"))
                cameraPos.X -= panAmount / camera.Scale.X;
            if (input.IsKeyBindingActive("pan_right"))
                cameraPos.X += panAmount / camera.Scale.X;
            if (input.IsKeyBindingActive("zoom_out"))
                camera.Scale /= 1.05f;
            if (input.IsKeyBindingActive("zoom_in"))
                camera.Scale *= 1.05f;

            if (input.IsKeyBindingPress("toggle_coloring"))
                colored = !colored;
            if (input.IsKeyBindingActive("pen_add"))
            {
                Vector2 pos = input.GetMousePos();
                pos = camera.Screen2WorldCoords(pos);
                pen.Draw(map, pos);
            }

            if (input.IsKeyBindingActive("pen_sub"))
            {
                Vector2 pos = input.GetMousePos();
                pos = camera.Screen2WorldCoords(pos);
                subPen.Draw(map, pos);
            }
                

            camera.LockPosition(cameraPos, true);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            batch.Begin(0, BlendState.AlphaBlend, null, null, null, null, camera.TransformMatrix);
            batch.Draw(map.GetTexture(colored), Vector2.Zero, Color.White);
            batch.End();

            camera.AdvanceFrame();
            base.Draw(gameTime);
        }
    }
}
