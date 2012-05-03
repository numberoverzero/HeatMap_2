using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
namespace Engine.Utility
{
    public static class FunctionUtils
    {
        /// <summary>
        /// Action that does nothing.
        /// </summary>
        public static Action NoneAction
        {
            get { return () => { }; }
        }
    }

    /// <summary>
    /// Creates single color textures (i.e. for drawing rectangles).
    /// </summary>
    public class ColorTexture
    {
        /// <summary>
        /// Creates a 1x1 pixel black texture.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device to use.</param>
        /// <returns>The newly created texture.</returns>
        public static Texture2D Create(GraphicsDevice graphicsDevice)
        {
            return Create(graphicsDevice, 1, 1, new Color());
        }

        /// <summary>
        /// Creates a 1x1 pixel texture of the specified color.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device to use.</param>
        /// <param name="color">The color to set the texture to.</param>
        /// <returns>The newly created texture.</returns>
        public static Texture2D Create(GraphicsDevice graphicsDevice, Color color)
        {
            return Create(graphicsDevice, 1, 1, color);
        }

        /// <summary>
        /// Creates a texture of the specified color.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device to use.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="color">The color to set the texture to.</param>
        /// <returns>The newly created texture.</returns>
        public static Texture2D Create(GraphicsDevice graphicsDevice, int width, int height, Color color)
        {
            // create the rectangle texture without colors
            Texture2D texture = new Texture2D(graphicsDevice, width, height);

            // Create a color array for the pixels
            Color[] colors = new Color[width * height];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color(color.ToVector3());
            }

            // Set the color data for the texture
            texture.SetData(colors);

            return texture;
        }

        public static RenderTarget2D CreateRenderTarget(GraphicsDevice graphicsDevice, int width, int height, bool preserveContents)
        {
            PresentationParameters pp = graphicsDevice.PresentationParameters;
            var usage = preserveContents ? RenderTargetUsage.PreserveContents : RenderTargetUsage.DiscardContents;
            return new RenderTarget2D(graphicsDevice, width, height, false, pp.BackBufferFormat, pp.DepthStencilFormat, pp.MultiSampleCount, usage);
        }

        public static RenderTarget2D CreateRenderTarget(GraphicsDevice graphicsDevice, int width, int height, RenderTargetUsage usage)
        {
            PresentationParameters pp = graphicsDevice.PresentationParameters;
            return new RenderTarget2D(graphicsDevice, width, height, false, pp.BackBufferFormat, pp.DepthStencilFormat, pp.MultiSampleCount, usage);
        }
    }

    public static class ShaderUtil
    {
        static GraphicsDevice graphicsDevice;
        static SpriteBatch spriteBatch;

        public static void LoadContent(GraphicsDevice graphicsDevice)
        {
            ShaderUtil.graphicsDevice = graphicsDevice;
            ShaderUtil.spriteBatch = new SpriteBatch(graphicsDevice);
        }
        public static void DrawFullscreenQuad(Texture2D texture, RenderTarget2D renderTarget, BlendState blendState, Effect effect)
        {
            graphicsDevice.SetRenderTarget(renderTarget);
            DrawFullscreenQuad(texture, blendState, renderTarget.Width, renderTarget.Height, effect);
        }

        public static void DrawFullscreenQuad(Texture2D texture, BlendState blendState, int width, int height, Effect effect)
        {
            spriteBatch.Begin(0, blendState, null, null, null, effect);
            spriteBatch.Draw(texture, new Rectangle(0, 0, width, height), Color.White);
            spriteBatch.End();
        }
    }
    public static class BasicShapes
    {
        private static GraphicsDevice graphicsDevice;
        private static Texture2D pixel1x1;
        public static void Initialize(GraphicsDevice graphicsDevice)
        {
            if(BasicShapes.graphicsDevice == null)
                BasicShapes.graphicsDevice = graphicsDevice;
            if (pixel1x1 == null)
                pixel1x1 = ColorTexture.Create(graphicsDevice, 1, 1, Color.White);
        }

        /// <summary>
        /// Draws a filled square whose center is position
        /// </summary>
        /// <param name="batch">SpriteBatch for drawing</param>
        /// <param name="position">Center of the Square</param>
        /// <param name="color">Color of the square</param>
        /// <param name="width">Square side length</param>
        /// <param name="rotation">Rotation in radians about the center</param>
        public static void DrawSolidSquare(SpriteBatch batch, Vector2 position, Color color, float width, float rotation)
        {
            var drawPosition = position - new Vector2(width / 2);
            drawPosition = drawPosition.RotateAbout(position, rotation);
            batch.Draw(pixel1x1, drawPosition, null, color, rotation, Vector2.Zero, width, SpriteEffects.None, 0);
        }

        /// <summary>
        /// Draws a hollow square whose center is position, of width lineWidth (default 1px, does not exceed width)
        /// </summary>
        /// <param name="batch">SpriteBatch for drawing</param>
        /// <param name="position">Center of the Square</param>
        /// <param name="color">Color of the square</param>
        /// <param name="width">Square side length</param>
        /// <param name="rotation">Rotation in radians about the center</param>
        /// <param name="lineWidth">Width in pixels of the line</param>
        public static void DrawSquareOutline(SpriteBatch batch, Vector2 position, Color color, float width, float rotation, float lineWidth = 1)
        {
            DrawRectangleOutline(batch, position, color, new Vector2(width, width), rotation, lineWidth);
        }

        public static void DrawRectangleOutline(SpriteBatch batch, Vector2 position, Color color, Vector2 dimensions, float rotation, float lineWidth = 1)
        {
            Vector2 dim2 = dimensions / 2;
            Vector2[] corners = new Vector2[] {position + new Vector2(-dim2.X + lineWidth, -dim2.Y + lineWidth),
                                               position + new Vector2(-dim2.X + lineWidth, dim2.Y - lineWidth),
                                               position + new Vector2(dim2.X - lineWidth, dim2.Y - lineWidth),
                                               position + new Vector2(dim2.X - lineWidth, -dim2.Y + lineWidth),
                                               position + new Vector2(-dim2.X + lineWidth, -dim2.Y + lineWidth)
                                              };

            for (int i = 0; i < 5; i++)
            {
                corners[i] = corners[i].RotateAbout(position, rotation);
            }

            Vector2 scale = new Vector2(0, lineWidth);
            Vector2 segment;
            for (int i = 0; i < 4; i++)
            {
                segment = (corners[i + 1] - corners[i]);
                scale.X = segment.Length() + lineWidth;
                batch.Draw(pixel1x1, corners[i], null, color, segment.AsAngle(), Vector2.Zero, scale, SpriteEffects.None, 0);
            }
        }
    }

    /// <summary>
    /// Extensions for Vector2, such as Dot and Random
    /// </summary>
    public static class Vector2Extensions
    {
        static Random random = new Random();
        
        /// <summary>
        /// Returns a Vector2 with values s.t Length() in [0, limit]
        /// </summary>
        /// <param name="limit"></param>
        /// <returns></returns>
        public static Vector2 Random(this Vector2 vector, float limit)
        {
            var rx = random.NextDouble();
            var ry = random.NextDouble();
            var vec = new Vector2((float)rx, (float)ry);
            vec.Normalize();
            var mag = limit * (float)random.NextDouble();
            return vec * mag;
        }

        public static Vector2 RandomAtLimit(this Vector2 vector, float limit)
        {
            var rx = random.NextDouble();
            var ry = random.NextDouble();
            var vec = new Vector2((float)rx, (float)ry);
            vec.Normalize();
            return vec * limit;
        }

        public static Vector2 Rotate(this Vector2 vector, double theta)
        {
            Vector2 outvec = Vector2.Zero;
            double nx, ny;
            nx = Math.Cos(theta) * vector.X - Math.Sin(theta) * vector.Y;
            ny = Math.Sin(theta) * vector.X + Math.Cos(theta) * vector.Y;
            outvec.X = (float)nx;
            outvec.Y = (float)ny;
            return outvec;
        }

        public static Vector2 RotateAbout(this Vector2 vector, Vector2 origin, float theta)
        {
            Vector2 outvec = Vector2.Zero;
            outvec.X = origin.X + (float)(Math.Cos(theta) * (vector.X - origin.X) - Math.Sin(theta) * (vector.Y - origin.Y));
            outvec.Y = origin.Y + (float)(Math.Sin(theta) * (vector.X - origin.X) + Math.Cos(theta) * (vector.Y - origin.Y));
            return outvec;
        }
        
        /// <summary>
        /// Counter-Clockwise tangent
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector2 UnitTangent1(this Vector2 vector)
        {
            var vec = vector.Rotate(MathHelper.PiOver2);
            vec.Normalize();
            return vec;
        }

        /// <summary>
        /// Dot product
        /// </summary>
        /// <param name="self"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static float Dot(this Vector2 self, Vector2 other)
        {
            return self.X * other.X + self.Y * other.Y;
        }

        /// <summary>
        /// Clockwise tangent
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector2 UnitTangent2(this Vector2 vector)
        {
            var vec = vector.Rotate(-MathHelper.PiOver2);
            vec.Normalize();
            return vec;
        }

        public static float AsAngle(this Vector2 vector)
        {
            return (float)Math.Atan2(vector.Y, vector.X);
        }
    }

    public static class MathUtil
    {
        /// <summary>
        /// Returns the minimum value in an array of floats
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static float Min(float[] values)
        {
            var min = float.MaxValue;
            foreach (var val in values)
                if (val < min)
                    min = val;
            return min;
        }

        /// <summary>
        /// Returns the minimum and maximum values in an array of floats
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static Vector2 MinMax(float[] values)
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            foreach (var val in values)
            {
                if (val < min)
                    min = val;
                if (val > max)
                    max = val;
            }   
            return new Vector2(min, max);
        }

        public static float Squared(this float value)
        {
            return (float)Math.Pow(value, 2);
        }

        public static float Average(params float[] values)
        {
            return values.Sum() / values.Count();
        }
    }
    /// <summary>
    /// Extensions for floats and doubles
    /// </summary>
    public static class FloatAndDoubleExentsions
    {
        static Random random = new Random();

        public static float Clamp(this float val, float min, float max)
        {
            return MathHelper.Clamp(val, min, max);
        }

        public static double Clamp(this double val, double min, double max)
        {
            return MathHelper.Clamp((float)val, (float)min, (float)max);
        }

        public static float Random(this double val)
        {
            return (float)random.NextDouble();
        }
        public static float Random(this double val, double min, double max)
        {
            return MathHelper.Lerp((float)min, (float)max, (float)random.NextDouble());
        }
    }

    public static class EnumUtil
    {
        public static IEnumerable<T> GetValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }
    }

    public static class StringExtensions
    {
        public static string[] Split (this string s, string delimeter, bool includeEmptyString = false)
        {
            var options = includeEmptyString ? StringSplitOptions.None : StringSplitOptions.RemoveEmptyEntries;
            return s.Split(new string[]{delimeter}, options);
        }
    }
    public interface MathFunction
    {
        float At(float t);
    }

    public class OscillatingFunction : MathFunction
    {
        float initial;
        float mag;
        float freq;

        public OscillatingFunction(float initial, float mag, float freq)
        {
            this.initial = initial;
            this.mag = mag;
            this.freq = freq;
        }

        public virtual float At(float t)
        {
            float offset = mag * (float)Math.Sin(t * freq * 2 * Math.PI);
            return initial + offset;
        }
    }

    public class DecayFunction : MathFunction
    {
        float initial;
        float mag;
        float decayCoeff;

        public DecayFunction(float initial, float mag, float decayCoeff)
        {
            this.initial = initial;
            this.mag = mag;
            this.decayCoeff = decayCoeff;
        }

        public virtual float At(float t)
        {
            float offset = mag * (float) Math.Exp(-decayCoeff * t);
            return initial + offset;
        }

    }

    public class SmoothStepFunction : MathFunction
    {
        public virtual float At(float t)
        {
            return t * t * (3 - 2 * t);
        }
    }
}