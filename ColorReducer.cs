using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace GKProj3
{
    public abstract class ColorReducer
    {
        protected const int R = 0;
        protected const int G = 1;
        protected const int B = 2;

        protected double[,,] sourceBitmapData;
        protected int imageWidth;
        protected int imageHeight;

        public ColorReducer(Bitmap baseImage)
        {
            imageWidth = baseImage.Width;
            imageHeight = baseImage.Height;
            sourceBitmapData = new double[3, imageWidth, imageHeight];

            for(int x = 0; x < imageWidth; x++)
            {
                for(int y = 0; y < imageHeight; y++)
                {
                    Color c = baseImage.GetPixel(x, y);
                    sourceBitmapData[R, x, y] = c.R;
                    sourceBitmapData[G, x, y] = c.G;
                    sourceBitmapData[B, x, y] = c.B;
                }
            }
        }
        public abstract Bitmap Reduce(int ColorN);
    }

    public class ErrorDiffusionColorReducer : ColorReducer
    {
        private double[,] filter;
        private double filterSum;
        private int fx,fy; // width and height of filter matrix (from the middle) 

        public enum Modes
        {
            FloydSteinberg,
            Burkes,
            Stucky
        }


        public ErrorDiffusionColorReducer(Bitmap baseImage, Modes mode ) : base(baseImage)
        {

            switch (mode)
            {
                case Modes.FloydSteinberg:
                    {
                        filter = new double[3, 3]
                        {
                            {0, 0, 3},
                            {0, 0, 5},
                            {0, 7, 1}
                        };
                        filterSum = 16;
                        fx = fy = 1;
                        break;
                    }
                case Modes.Burkes:
                    {
                        filter = new double[5, 3]
                        {
                            {0, 0, 2 },
                            {0, 0, 4 },
                            {0, 0, 8 },
                            {0, 8, 4 },
                            {0, 4, 2 }
                        };
                        filterSum = 32;
                        fx = 2;
                        fy = 1;
                        break;
                    }
                case Modes.Stucky:
                    {
                        filter = new double[5, 5]
                        {
                            {0, 0, 0, 2, 1},
                            {0, 0, 0, 4, 2},
                            {0, 0, 0, 8, 4},
                            {0, 0, 8, 4, 2},
                            {0, 0, 4, 2, 1}
                        };
                        filterSum = 42;
                        fx = fy = 2;
                        break;
                    }
                default:
                    throw new Exception("worng filter mode");
            }
        }

        public override Bitmap Reduce(int colorN)
        {
            int singleChannelN = (int)Math.Max(1, Math.Pow(colorN, (double)1 / 3));

            return Reduce(singleChannelN);
        }

        public Bitmap Reduce(int rN, int gN, int bN)
        {
            Bitmap reducedImage = new Bitmap(imageWidth, imageHeight);

            // main loop
            for (int x = 0; x < imageWidth; x++)
            {
                for (int y = 0; y < imageHeight; y++)
                {
                    Color colorToDisplay = Color.FromArgb(
                        QuantizeChannel(sourceBitmapData[R, x, y], rN),
                        QuantizeChannel(sourceBitmapData[G, x, y], gN),
                        QuantizeChannel(sourceBitmapData[B, x, y], bN)
                        );

                    reducedImage.SetPixel(x, y, colorToDisplay);

                    double errorR = sourceBitmapData[R, x, y] - colorToDisplay.R;
                    double errorG = sourceBitmapData[G, x, y] - colorToDisplay.G;
                    double errorB = sourceBitmapData[B, x, y] - colorToDisplay.B;


                    for (int i = -fx; i <= fx; i++)
                    {
                        for (int j = -fy; j <= fy; j++)
                        {
                            if (x + i < 0 || x + i >= imageWidth || y + j < 0 || y + j >= imageHeight)
                                continue;



                            sourceBitmapData[R, x + i, y + j] += (errorR / filterSum) * filter[fx + i, fy + j];
                            sourceBitmapData[G, x + i, y + j] += (errorG / filterSum) * filter[fx + i, fy + j];
                            sourceBitmapData[B, x + i, y + j] += (errorB / filterSum) * filter[fx + i, fy + j];
                        }
                    }
                }
            }

            return reducedImage;
        }

        private static int QuantizeChannel(double value, int levelsN)
        {
            if(levelsN <= 1)
            {
                return 255;
            }

            double step = 255.0 / (levelsN - 1);
            double k = value / step;
            int r = (int)k;
            return (int)((k > r + 0.5 ? r + 1 : r) * step);
        }
    }

    public class PopularityColorReducer : ColorReducer
    {
        private int[] colorCounts;
        private int[] indexes;
        private Color[] displayColors;
        public PopularityColorReducer(Bitmap baseImage) : base(baseImage)
        {
            colorCounts = new int[256 * 256 * 256];
            indexes = Enumerable.Range(0, 256 * 256 * 256).ToArray();

            
            for (int x = 0; x < imageWidth; x++)
            {
                for (int y = 0; y < imageHeight; y++)
                {
                    int color = ((int)sourceBitmapData[R,x,y] << 16) | ((int)sourceBitmapData[G,x,y] << 8) | (int)sourceBitmapData[B,x,y];
                    colorCounts[color]++;
                }
            }

            Array.Sort(colorCounts,indexes,Comparer<int>.Create((c1,c2) => c2.CompareTo(c1)));
        }

        public override Bitmap Reduce(int colorN)
        {
            Bitmap resultBitmap = new Bitmap(imageWidth, imageHeight);


            displayColors = indexes.Take(colorN).Select(x => Color.FromArgb(255<<24 + x)).ToArray();

            for (int x = 0; x < imageWidth; x++)
            {
                for (int y = 0; y < imageHeight; y++)
                {
                    Color colorToDisplay = GetClosestDispalyColor(Color.FromArgb((int)sourceBitmapData[R, x, y], (int)sourceBitmapData[G, x, y], (int)sourceBitmapData[B,x,y]));

                    resultBitmap.SetPixel(x, y, colorToDisplay);
                }
            }
            return resultBitmap;
        }


        private Color GetClosestDispalyColor(Color c)
        {
            Color closestColor = displayColors[0];
            int minDistance = ColorDistance2(closestColor,c);

            for(int i=1; i< displayColors.Length && minDistance > 0; i++)
            {
                int dist = ColorDistance2(displayColors[i],c);
                if(dist < minDistance)
                {
                    minDistance = dist;
                    closestColor = displayColors[i];
                }
            }
            return closestColor;
        }

        private int ColorDistance2(Color c1, Color c2)
        {
            return (c2.R - c1.R) * (c2.R - c1.R) + (c2.G - c1.G) * (c2.G - c1.G) + (c2.B - c1.B)*(c2.B-c1.B);
        }

    }
    
}
