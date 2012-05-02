using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Engine.Utility;

namespace HeatMap
{
    public class Pen
    {
        public static MathFunction intensityFunc = new SmoothStepFunction();
        public float Radius;
        public float Max;
        public float Min;

        public Pen(float radius, float min, float max)
        {
            Radius = radius;
            Min = min;
            Max = max;
        }
        public void Draw(IArray2D array, Vector2 position)
        {
            // The +0.5f here is used to make sure we don't consider points that are known to be farther away than the Radius.
            // for example- if position.X = 5.5 and radius = 3, then (int)(5.5 - 3) = 2.  However, 2 is actually 3.5 from 5.5, so
            // we'll have an entire extra row to check that we didn't need to.  However, (int)(0.5 + 5.5 - 3) = 3 and we know 3 will be checked.
            int min_x = (int)(0.5f + position.X - Radius);
            int min_y = (int)(0.5f + position.Y - Radius);
            min_x = Math.Max(min_x, 0);
            min_y = Math.Max(min_y, 0);

            int max_x = (int)(position.X + Radius);
            int max_y = (int)(position.Y + Radius);
            max_x = Math.Min(max_x, array.GetWidth()-1);
            max_y = Math.Min(max_y, array.GetHeight()-1);

            float dist2, pct, offset, initial;
            // y is row counter, so it iterates over vertical
            for (int y = min_y; y <= max_y; y++)
            {
                // x is col counter, so it iterates over horizontal
                for (int x = min_x; x <= max_x; x++)
                {
                    dist2 = new Vector2(position.X - x, position.Y - y).LengthSquared();
                    if (dist2 > Radius*Radius) continue;
                    pct = Pen.intensityFunc.At(dist2 / (Radius*Radius));
                    offset = MathHelper.Lerp(Min, Max, 1-pct);
                    initial = array.GetValue(x, y);
                    array.SetValue(x, y, initial + offset);
                }
            }
        }
    }
}
