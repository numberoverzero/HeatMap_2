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
        Texture2D pixel1x1;
        SpriteBatch batch;
        SpriteFont font;

        float panAmount = 4.0f; 
        Vector2 cameraPos;
        Vector2 mousePos;
        Camera camera;
        Input input;

        int resolution = 9;
        int size;
        Map map;
        bool colored = true;
        Pen pen = new Pen(100, 0, 0.04f);
        Pen subPen = new Pen(100, 0, -0.04f);


        public Game()
        {
            IsMouseVisible = true;
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = graphics.PreferredBackBufferWidth = 512;
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
            input.AddKeyBinding("toggle_coloring", Keys.Tab);
            input.AddKeyBinding("cycle_map", Keys.Tab, Modifier.Shift);
            input.AddKeyBinding("generate_random_map", Keys.Space);

            // Pen
            input.AddKeyBinding("pen_add", MouseButton.Left);
            input.AddKeyBinding("pen_sub", MouseButton.Right);
            input.AddKeyBinding("pen_size_inc", Keys.Up);
            input.AddKeyBinding("pen_size_dec", Keys.Down);
            input.AddKeyBinding("pen_pressure_inc", Keys.Right);
            input.AddKeyBinding("pen_pressure_dec", Keys.Left);

            //Map Size
            input.AddKeyBinding("map_size_inc", Keys.OemCloseBrackets);
            input.AddKeyBinding("map_size_dec", Keys.OemOpenBrackets);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            batch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("DebugFont");

            pixel1x1 = new Texture2D(GraphicsDevice, 1, 1);
            pixel1x1.SetData(new Color[] { Color.White });

            ShaderUtil.LoadContent(GraphicsDevice); 
            
            camera = new Camera(GraphicsDevice.Viewport);
            cameraPos = new Vector2(256);
            
            Map.LoadContent(Content, GraphicsDevice);
            AdjustSize(0);
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            camera.Update(gameTime.ElapsedGameTime.Milliseconds / 1000f); 
            
            input.Update();
            mousePos = camera.Screen2WorldCoords(input.GetMousePos());

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
            if (input.IsKeyBindingPress("generate_random_map"))
                map.GenerateRandomHeight();

            if (input.IsKeyBindingActive("pen_add"))
            {
                map.ApplyPen(pen, mousePos);
            }
            if (input.IsKeyBindingActive("pen_sub"))
            {
                map.ApplyPen(subPen, mousePos);
            }
            if (input.IsKeyBindingActive("pen_size_inc"))
            {
                pen.Radius *= 1.01f;
                subPen.Radius *= 1.01f;
            }
            if (input.IsKeyBindingActive("pen_size_dec"))
            {
                pen.Radius /= 1.01f;
                subPen.Radius /= 1.01f;
            }
            if (input.IsKeyBindingActive("pen_pressure_inc"))
            {
                pen.Max *= 1.01f;
                subPen.Max *= 1.01f;
            }
            if (input.IsKeyBindingActive("pen_pressure_dec"))
            {
                pen.Max /= 1.01f;
                subPen.Max /= 1.01f;
            }

            if (input.IsKeyBindingPress("map_size_inc"))
                AdjustSize(1);
            if (input.IsKeyBindingPress("map_size_dec"))
                AdjustSize(-1);
                

            camera.LockPosition(cameraPos, true);
            base.Update(gameTime);
        }

        /// <summary>
        /// Adjusts the size of the map.  If there is already a map created, clears the old map and
        /// initializes a new one.  None of the data from the old map is preserved.
        /// </summary>
        /// <param name="offset"></param>
        private void AdjustSize(int offset)
        {
            resolution += offset;
            resolution = (int)MathHelper.Clamp(resolution, 1, 11);
            size = (int)Math.Pow(2, resolution) + 1;
            if (map != null)
                map.Destroy();
            map = new Map(size, size);
            map.AddColorMap(Map.DefaultColorMap);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            batch.Begin(0, BlendState.AlphaBlend, null, null, null, null, camera.TransformMatrix);
            batch.Draw(map.GetTexture(colored), Vector2.Zero, Color.White);
            batch.End();

            DrawPenInfo();
            if (map.IsGenerating) DrawGenerating();
            camera.AdvanceFrame();
            base.Draw(gameTime);
        }

        void DrawPenInfo()
        {
            string textFmt = "Map Size: ({0}, {1})\nPosition: ({2:0.00}, {3:0.00})\nRadius: {4:0.000}\nPressure: {5:0.00000}";
            string text = String.Format(textFmt, size, size, mousePos.X, mousePos.Y, pen.Radius, pen.Max);
            Vector2 textDimensions = font.MeasureString(text);
            Vector2 screenDimensions = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            Vector2 textPos = new Vector2(0, screenDimensions.Y - textDimensions.Y);
            DrawStringWithBackground(text, textPos);

        }

        void DrawGenerating()
        {
            string text = "Generating...";
            Vector2 textDimensions = font.MeasureString(text);
            Vector2 screenDimensions = new Vector2(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            Vector2 textPos = screenDimensions - textDimensions;
            DrawStringWithBackground(text, textPos);
        }

        void DrawStringWithBackground(string text, Vector2 textPos)
        {
            Vector2 textDimensions = font.MeasureString(text);
            batch.Begin();
            batch.Draw(pixel1x1, textPos, null, Color.Gray, 0, Vector2.Zero, textDimensions, SpriteEffects.None, 0);
            batch.DrawString(font, text, textPos, Color.White);
            batch.End();
        }
    }
}
