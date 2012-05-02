using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Threading;
using Engine.Utility;

namespace HeatMap
{
    public interface IArray2D
    {
        int GetWidth();
        int GetHeight();
        void SetWidth(int width);
        void SetHeight(int height);

        void SetValue(int row, int col, float value);
        float GetValue(int row, int col);
    }
    public class Map : IArray2D
    {
        #region Fields

        float[] data; 
        int width, height;
        bool _suppressClamping;
        bool _dirty;
        bool _isGenerating;
        bool IsGenerating
        {
            get { return _isGenerating; }
        }

        public static GraphicsDevice GraphicsDevice; 
        public static Effect ColorMapEffect;
        SpriteBatch batch;

        RenderTarget2D coloredTextureCache;
        Texture2D intensityTexture;

        public static Texture2D DefaultColorMap;
        List<Texture2D> colorMaps;

        int colorMapIndex;
        public int ColorMapIndex
        {
            get { return colorMapIndex; }
            set
            {
                int nColorMaps = colorMaps.Count();
                if (nColorMaps > 0)
                {
                    value %= colorMaps.Count();
                    colorMapIndex = value;
                } else
                    colorMapIndex = 0;
                _dirty = true;
            }
        }

        #endregion

        public Map(int width, int height)
        {
            batch = new SpriteBatch(GraphicsDevice);
            
            this.width = width;
            this.height = height;

            data = new float[width * height];
            for (int i = 0; i < width * height; i++) 
                data[i] = 0;

            _suppressClamping = false;
            _isGenerating = false;
            _dirty = true;
            
            coloredTextureCache = ColorTexture.CreateRenderTarget(GraphicsDevice, width, height, true);
            colorMaps = new List<Texture2D>();
            colorMapIndex = 0;
        }

        public float this[int row, int col]
        {
            get { return data[width * row + col]; }
            set
            {
                _dirty = true;
                if (!_suppressClamping)
                    value = MathHelper.Clamp(value, 0, 1);
                data[width * row + col] = value;
            }
        }

        #region GetTexture

        public Texture2D GetTexture(bool colored)
        {
            if (_dirty && !_isGenerating)
                RenderTextures();
            if (colored)
                return coloredTextureCache;
            return intensityTexture;
        }

        private void RenderTextures()
        {
            _dirty = false;
            if(intensityTexture == null)
                intensityTexture = new Texture2D(GraphicsDevice, width, height);
            Color[] intensityColors = new Color[width * height];
            float value;
            for (int i = 0; i < width * height; i++)
            {
                value = data[i];
                intensityColors[i] = new Color(value, value, value, value);
            }
            intensityTexture.SetData(intensityColors);

            GraphicsDevice.Textures[0] = intensityTexture;
            GraphicsDevice.Textures[1] = colorMaps[colorMapIndex];
            ShaderUtil.DrawFullscreenQuad(intensityTexture, coloredTextureCache, BlendState.AlphaBlend, ColorMapEffect);
            GraphicsDevice.SetRenderTarget(null);
        }

        #endregion

        #region Iterative Diamond-Square Algorithm

        private void GenerateRandomHeight(Func<float, float, int, float> noiseFunction)
        {
            float noiseMin = -1;
            float noiseMax = 1;
            _suppressClamping = true;
            for (int i = 0; i < width * height; i++)
                data[i] = 0;
            float corner = noiseFunction(noiseMin, noiseMax, 0);
            this[0, 0] = this[0, height - 1] = this[width - 1, 0] = this[width - 1, height - 1] = corner;

            int x_min = 0;
            int y_min = 0;
            int x_max = width-1;
            int y_max = height-1;

            int side = x_max;
            int squares = 1;
            int offset = 1;

            int left, right, top, bottom, dx, dy, midX, midY, temp;
            while (side > 1){
                for (int i = 0; i < squares; i++){
                    for (int j = 0; j < squares; j++){
                        left = i * side;
                        right = (i + 1) * side;
                        top = j * side;
                        bottom = (j + 1) * side;

                        dx = dy = side / 2;

                        midX = left + dx;
                        midY = top + dy;

                        // Diamond step - create center average for each square
                        this[midX, midY] = Average(this[left, top],
                                                   this[left, bottom],
                                                   this[right, top],
                                                   this[right, bottom]);
                        this[midX, midY] += noiseFunction(noiseMin, noiseMax, offset);

                        // Square step - create squares for each diamond

                        // ==============
                        // Top Square
                        if (top - dy < y_min)
                            temp = y_max - dy;
                        else
                            temp = top - dy;
                        this[midX, top] = Average(this[left, top],
                                                  this[right, top],
                                                  this[midX, midY],
                                                  this[midX, temp]);
                        this[midX, top] += noiseFunction(noiseMin, noiseMax, offset);

                        // Top Wrapping
                        if (top == y_min)
                            this[midX, y_max] = this[midX, top];

                        // ==============
                        // Bottom Square
                        if (bottom + dy > y_max)
                            temp = top + dy;
                        else
                            temp = bottom - dy;
                        this[midX, bottom] = Average(this[left, bottom],
                                                     this[right, bottom],
                                                     this[midX, midY],
                                                     this[midX, temp]);
                        this[midX, bottom] += noiseFunction(noiseMin, noiseMax, offset);

                        // Bottom Wrapping
                        if (bottom == y_max)
                            this[midX, y_min] = this[midX, bottom];

                        // ==============
                        // Left Square
                        if (left - dx < x_min)
                            temp = x_max - dx;
                        else
                            temp = left - dx;
                        this[left, midY] = Average(this[left, top],
                                                   this[left, bottom],
                                                   this[midX, midY],
                                                   this[temp, midY]);
                        this[left, midY] += noiseFunction(noiseMin, noiseMax, offset);

                        // Left Wrapping
                        if (left == x_min)
                            this[x_max, midY] = this[left, midY];

                        // ==============
                        // Right Square
                        if (right + dx > x_max)
                            temp = x_min + dx;
                        else
                            temp = right + dx;
                        this[right, midY] = Average(this[right, top],
                                                    this[right, bottom],
                                                    this[midX, midY],
                                                    this[temp, midY]);
                        this[right, midY] += noiseFunction(noiseMin, noiseMax, offset);

                        // Right Wrapping
                        if (right == x_max)
                            this[x_min, midY] = this[right, midY];
                    }
                } //End for loops
                side /= 2;
                squares *= 2;
                offset += 1;
            }
            Normalize();
            _suppressClamping = false;
            _isGenerating = false;
        }

        public void ThreadedGenerateRandomHeight(Func<float, float, int, float> noiseFunction)
        {
            if (_isGenerating) 
                return;
            _isGenerating = true;
            Thread t = new Thread(() => { GenerateRandomHeight(noiseFunction); });
            t.Start();
        }

        #endregion

        #region ColorMaps

        public void AddColorMap(Texture2D colorMap)
        {
            colorMaps.Add(colorMap);
        }

        public void RemoveColorMap(Texture2D colorMap)
        {
            colorMaps.Remove(colorMap);
        }

        #endregion

        #region Private Helpers

        private void Normalize()
        {
            float minValue = data.Min();
            float maxValue = data.Max();
            float range = maxValue - minValue;
            float pct = 0.05f;
            minValue -= range * pct / 2;
            maxValue += range * pct / 2;
            range = maxValue-minValue;
            for (int i = 0; i < width * height; i++)
                data[i] = (data[i] - minValue) / range;

            _dirty = true;
        }

        private static float Average(params float[] values)
        {
            return values.Sum() / values.Count();
        }

        #endregion

        public static void LoadContent(ContentManager content, GraphicsDevice device)
        {
            DefaultColorMap = content.Load<Texture2D>("ColorMaps/DefaultColorMap");
            ColorMapEffect = content.Load<Effect>("ColorMapEffect");
            GraphicsDevice = device;
        }

        public int GetWidth()
        {
            return width;
        }

        public int GetHeight()
        {
            return height;
        }

        public void SetWidth(int width)
        {
            this.width = width;
        }

        public void SetHeight(int height)
        {
            this.height = height;
        }

        public void SetValue(int row, int col, float value)
        {
            this[row, col] = value;
        }

        public float GetValue(int row, int col)
        {
            return this[row, col];
        }
    }
}
