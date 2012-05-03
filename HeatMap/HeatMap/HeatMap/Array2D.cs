using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
namespace HeatMap
{
    public class Array2D
    {
        int width, height;
        public int Width
        {
            get { return width; }
        }
        public int Height
        {
            get { return height; }
        }

        float[] data;
        public float[] Data
        {
            get { return data; }
        }
        
        public float this[int row, int col]
        {
            get { return data[width * row + col]; }
            set
            {
                data[width * row + col] = value;
            }
        }

        public Array2D(int width, int height, float initalValue = 0)
        {
            this.width = width;
            this.height = height;
            this.data = new float[width * height];
            Clear(initalValue);
        }

        public void Clear(float value)
        {
            for (int i = 0; i < width * height; i++)
                data[i] = value;
        }

        public void Normalize(float min, float max)
        {
            float minDataValue = data.Min();
            float maxDataValue = data.Max();
            float dataRange = maxDataValue - minDataValue;

            float t;
            for (int i = 0; i < width * height; i++)
            {
                t = (data[i] - minDataValue) / dataRange;
                data[i] = MathHelper.Lerp(min, max, t);
            }
        }

        public Color[] AsAlphaMap()
        {
            Color[] alphaMap = new Color[width * height];
            for (int i = 0; i < width * height; i++)
                alphaMap[i] = new Color(0, 0, 0, data[i]);
            return alphaMap;
        }
    }
}
