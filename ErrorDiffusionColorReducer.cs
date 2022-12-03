using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GKProj3
{
    public class ErrorDiffusionColorReducer // Cannot inherit form Color Reducer because we need pixels as triples of doubles (to precisely diffuse the error)
    {
        private double[] pixels;
        private int[,] filter;
        private int filterSum;
        private int fx, fy; // width and height of filter matrix (from the middle) 
        private int imageWidth;
        private int imageHeight;

        public enum Modes
        {
            FloydSteinberg,
            Burkes,
            Stucky
        }

        public ErrorDiffusionColorReducer(Bitmap baseImage, Modes mode)
        {
            imageWidth = baseImage.Width;
            imageHeight = baseImage.Height;

            pixels = new double[imageWidth * imageHeight * 3];

            int pixelIdx = 0;
            for (int y = 0; y < imageHeight; y++)
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    Color pixelColor = baseImage.GetPixel(x, y);

                    pixels[pixelIdx] = pixelColor.R;
                    pixels[pixelIdx + 1] = pixelColor.G;
                    pixels[pixelIdx + 2] = pixelColor.B;
                    pixelIdx += 3;
                }
            }

            switch (mode)
            {
                case Modes.FloydSteinberg:
                    {
                        filter = new int[3, 3]
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
                        filter = new int[5, 3]
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
                        filter = new int[5, 5]
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

        public async Task<Bitmap> ReduceAsync(int rk, int gk, int bk)
        {
            Bitmap reducedImage = new Bitmap(imageWidth, imageHeight);

            // main loop
            int pixelIdx = 0;
             for (int y = 0; y < imageHeight; y++)
             {
                for (int x = 0; x < imageWidth; x++)
                {
                    Color colorToDisplay = Color.FromArgb(
                        QuantizeChannel(pixels[pixelIdx], rk),
                        QuantizeChannel(pixels[pixelIdx+1], gk),
                        QuantizeChannel(pixels[pixelIdx+2], bk)
                        );

                    reducedImage.SetPixel(x, y, colorToDisplay);

                    double errorR = pixels[pixelIdx] - colorToDisplay.R;
                    double errorG = pixels[pixelIdx+1] - colorToDisplay.G;
                    double errorB = pixels[pixelIdx+2] - colorToDisplay.B;


                    for (int i = -fx; i <= fx; i++)
                    {
                        for (int j = -fy; j <= fy; j++)
                        {
                            if (x + i < 0 || x + i >= imageWidth || y + j < 0 || y + j >= imageHeight)
                                continue;

                            int updateIdx = pixelIdx + (i + j * imageWidth) * 3;

                            pixels[updateIdx] += (errorR / filterSum) * filter[fx + i, fy + j];
                            pixels[updateIdx+1] += (errorG / filterSum) * filter[fx + i, fy + j];
                            pixels[updateIdx+2] += (errorB / filterSum) * filter[fx + i, fy + j];
                        }
                    }
                    pixelIdx += 3;
                }
            }

            return await Task.FromResult(reducedImage);
        }

        private static int QuantizeChannel(double value, int levelsN)
        {
            if (levelsN <= 1)
            {
                return 0;
            }

            double step = 255.0 / (levelsN - 1);
            double k = value / step;
            int r = (int)k;
            return (int)((k > r + 0.5 ? r + 1 : r) * step);
        }
    }
}
