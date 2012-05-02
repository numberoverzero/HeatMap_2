using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace HeatMap
{
    public static class MapGenerator
    {
        public static Map GenerateRandom(int width, int height, Func<float, float, int, float> noiseFunction)
        {
            Map map = new Map(width, height);
            map.ThreadedGenerateRandomHeight(noiseFunction);
            return map;
        }

        public static Map GenerateUniform(int width, int height, float value)
        {
            Map map = new Map(width, height);
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    map[j, i] = value;
            return new Map(width, height);
        }
    }
}
