using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Engine.Utility;

namespace HeatMap
{
    public static class MapGenerator
    {
        static Random random = new Random(0);
        static Thread generatorThread;

        public static void GenerateRandomHeight(Array2D array, Func<float, float, int, float> noiseFunction)
        {
            float noiseMin = -1;
            float noiseMax = 1;
            array.Clear(0);
            float corner = noiseFunction(noiseMin, noiseMax, 0);
            array[0, 0] = array[0, array.Height - 1] = array[array.Width - 1, 0] = array[array.Width - 1, array.Height - 1] = corner;

            int x_min = 0;
            int y_min = 0;
            int x_max = array.Width - 1;
            int y_max = array.Height - 1;

            int side = x_max;
            int squares = 1;
            int offset = 1;

            int left, right, top, bottom, dx, dy, midX, midY, temp;
            while (side > 1)
            {
                for (int i = 0; i < squares; i++)
                {
                    for (int j = 0; j < squares; j++)
                    {
                        left = i * side;
                        right = (i + 1) * side;
                        top = j * side;
                        bottom = (j + 1) * side;

                        dx = dy = side / 2;

                        midX = left + dx;
                        midY = top + dy;

                        // Diamond step - create center average for each square
                        array[midX, midY] = MathUtil.Average(array[left, top],
                                                   array[left, bottom],
                                                   array[right, top],
                                                   array[right, bottom]);
                        array[midX, midY] += noiseFunction(noiseMin, noiseMax, offset);

                        // Square step - create squares for each diamond

                        // ==============
                        // Top Square
                        if (top - dy < y_min)
                            temp = y_max - dy;
                        else
                            temp = top - dy;
                        array[midX, top] = MathUtil.Average(array[left, top],
                                                  array[right, top],
                                                  array[midX, midY],
                                                  array[midX, temp]);
                        array[midX, top] += noiseFunction(noiseMin, noiseMax, offset);

                        // Top Wrapping
                        if (top == y_min)
                            array[midX, y_max] = array[midX, top];

                        // ==============
                        // Bottom Square
                        if (bottom + dy > y_max)
                            temp = top + dy;
                        else
                            temp = bottom - dy;
                        array[midX, bottom] = MathUtil.Average(array[left, bottom],
                                                     array[right, bottom],
                                                     array[midX, midY],
                                                     array[midX, temp]);
                        array[midX, bottom] += noiseFunction(noiseMin, noiseMax, offset);

                        // Bottom Wrapping
                        if (bottom == y_max)
                            array[midX, y_min] = array[midX, bottom];

                        // ==============
                        // Left Square
                        if (left - dx < x_min)
                            temp = x_max - dx;
                        else
                            temp = left - dx;
                        array[left, midY] = MathUtil.Average(array[left, top],
                                                   array[left, bottom],
                                                   array[midX, midY],
                                                   array[temp, midY]);
                        array[left, midY] += noiseFunction(noiseMin, noiseMax, offset);

                        // Left Wrapping
                        if (left == x_min)
                            array[x_max, midY] = array[left, midY];

                        // ==============
                        // Right Square
                        if (right + dx > x_max)
                            temp = x_min + dx;
                        else
                            temp = right + dx;
                        array[right, midY] = MathUtil.Average(array[right, top],
                                                    array[right, bottom],
                                                    array[midX, midY],
                                                    array[temp, midY]);
                        array[right, midY] += noiseFunction(noiseMin, noiseMax, offset);

                        // Right Wrapping
                        if (right == x_max)
                            array[x_min, midY] = array[right, midY];
                    }
                } //End for loops
                side /= 2;
                squares *= 2;
                offset += 1;
            }
            array.Normalize(0,1);
        }

        public static void ThreadedGenerateRandomHeight(Array2D array, Func<float, float, int, float> noiseFunction, Action onComplete=null)
        {
            if (generatorThread != null) 
                return;
            if (onComplete == null) 
                onComplete = FunctionUtils.NoneAction;
            generatorThread = new Thread(() => { GenerateRandomHeight(array, noiseFunction); onComplete(); generatorThread = null; });
            generatorThread.Start();
        }

        public static float HeightFunction(float min, float max, int iteration)
        {
            float powerOffset = (float)Math.Pow(2, -iteration);
            float randomValue = (float)random.NextDouble();
            return MathHelper.Lerp(min * powerOffset, max * powerOffset, randomValue);
        }
    }
}
