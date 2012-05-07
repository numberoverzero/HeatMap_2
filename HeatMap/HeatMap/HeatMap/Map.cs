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
    public class Map
    {
        #region Fields

        int width, height;

        public static GraphicsDevice GraphicsDevice;
        public static Effect IntensityPenEffect;
        public static Effect ColorMapEffect;

        // Cached textures
        RenderTarget2D coloredTextureCache;
        RenderTarget2D intensityTextureCache;
        
        // Used for interactive manipulation
        RenderTarget2D intensityTextureTemp; 
        RenderTarget2D intensityTexture;
        
        bool _dirty;
        bool _generating;
        public bool IsGenerating
        {
            get { return _generating; }
        }

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
            this.width = width;
            this.height = height;
            _dirty = true;
            _generating = false;

            coloredTextureCache = ColorTexture.CreateRenderTarget(GraphicsDevice, width, height, true);
            intensityTextureCache = ColorTexture.CreateRenderTarget(GraphicsDevice, width, height, true);
            intensityTextureTemp = ColorTexture.CreateRenderTarget(GraphicsDevice, width, height, true);
            intensityTexture = ColorTexture.CreateRenderTarget(GraphicsDevice, width, height, true);

            colorMaps = new List<Texture2D>();
            colorMapIndex = 0;
        }

        #region GetTexture

        public Texture2D GetTexture(bool colored)
        {
            if (_dirty) RenderTextures();
            return colored ? coloredTextureCache : intensityTextureCache;
        }

        private void RenderTextures()
        {
            // Pass 1: copy intensityTexture to intensityTextureCache
            ShaderUtil.DrawFullscreenQuad(intensityTexture, intensityTextureCache, BlendState.Opaque, null);

            // Pass 2: apply ColorMap to intensityTexture and render out to coloredTextureCache
            GraphicsDevice.Textures[0] = intensityTexture;
            GraphicsDevice.Textures[1] = colorMaps[colorMapIndex];
            ShaderUtil.DrawFullscreenQuad(intensityTexture, coloredTextureCache, BlendState.AlphaBlend, ColorMapEffect);

            // Reset the render target
            GraphicsDevice.SetRenderTarget(null);

            _dirty = false;
        }

        #endregion

        #region Interaction

        public void ApplyPen(Pen pen, Vector2 position)
        {
            EffectParameterCollection parameters = IntensityPenEffect.Parameters;
            
            // Transform position coordinates into texture space
            position.X /= width;
            position.Y /= height;

            parameters["pos"].SetValue(position);
            parameters["radius"].SetValue(pen.Radius / Math.Max(width, height));
            parameters["minPressure"].SetValue(pen.Min);
            parameters["maxPressure"].SetValue(pen.Max);

            // Pass 1: apply PenEffect to intensityTexture and render out to intensityTextureTemp
            GraphicsDevice.Textures[0] = intensityTexture;
            ShaderUtil.DrawFullscreenQuad(intensityTexture, intensityTextureTemp, BlendState.Opaque, IntensityPenEffect);

            // Pass 2: copy intensityTextureTemp to intensityTexture
            ShaderUtil.DrawFullscreenQuad(intensityTextureTemp, intensityTexture, BlendState.Opaque, null);

            // Reset the render target
            GraphicsDevice.SetRenderTarget(null);

            _dirty = true;
        }

        #endregion

        #region Random Generation

        public void GenerateRandomHeight()
        {
            if (_generating) return;
            _generating = true;
            Array2D array = new Array2D(width, height, 0);
            Action onComplete = () =>
            {
                intensityTexture.SetData(array.AsAlphaMap());
                _dirty = true;
                _generating = false;
            }; 
            
            MapGenerator.ThreadedGenerateRandomHeight(array, MapGenerator.HeightFunction, onComplete);
            
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

        public static void LoadContent(ContentManager content, GraphicsDevice device)
        {
            DefaultColorMap = content.Load<Texture2D>("ColorMaps/DefaultColorMap");
            ColorMapEffect = content.Load<Effect>("ColorMapEffect");
            IntensityPenEffect = content.Load<Effect>("IntensityPenEffect");
            GraphicsDevice = device;
        }

        public void Destroy()
        {   
            coloredTextureCache = null;
            intensityTextureCache = null;
            intensityTextureTemp = null;
            intensityTexture = null;
        }
    }
}
