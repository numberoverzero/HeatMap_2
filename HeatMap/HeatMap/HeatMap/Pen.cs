using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Engine.Utility;

namespace HeatMap
{
    public struct Pen
    {
        public float Radius;
        public float Max;
        public float Min;

        public Pen(float radius, float min, float max)
        {
            Min = min;
            Max = max;
            Radius = radius;
        }
    }
}
