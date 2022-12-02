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
        protected byte[] pixels;
        protected int imageWidth;
        protected int imageHeight;
        //protected int bytesPerPixel;
        public ColorReducer(Bitmap baseImage)
        {
            imageWidth = baseImage.Width;
            imageHeight = baseImage.Height;
            //bytesPerPixel = (Image.GetPixelFormatSize(baseImage.PixelFormat) + 7 )/ 8;
            //ImageConverter converter = new ImageConverter();
            //pixels = (byte[])converter.ConvertTo(baseImage, typeof(byte[]))!;

            pixels = new byte[imageWidth * imageHeight * 3];
            
            int pixelIdx = 0;
            for (int y = 0; y < imageHeight; y++)
            {
                for(int x = 0; x < imageWidth; x++)
                {
                    Color pixelColor = baseImage.GetPixel(x, y);

                    pixels[pixelIdx] = pixelColor.R;
                    pixels[pixelIdx+1] = pixelColor.G;
                    pixels[pixelIdx+2] = pixelColor.B;
                    pixelIdx += 3;
                }
            }

        }
        public abstract Bitmap Reduce(int ColorN);
    }

    public class PopularityColorReducer : ColorReducer
    {
        private int[] colorCount;
        private int[] indexes;
        private Color[] displayColors;
        public PopularityColorReducer(Bitmap baseImage) : base(baseImage)
        {
            colorCount = new int[256*256*256];
            indexes = Enumerable.Range(0, 256 * 256 * 256).ToArray();

            for(int i=0; i<pixels.Length; i+=3)
            {
                int colorIdx = (pixels[i] <<16) | (pixels[i+1] << 8) | pixels[i+2];
                colorCount[colorIdx]++;
            }

            Array.Sort(colorCount, indexes, Comparer<int>.Create((int x, int y) => y.CompareTo(x)));
            displayColors = new Color[0];
        }

        public override Bitmap Reduce(int colorN)
        {
            displayColors = indexes.Take(colorN).Select(idx => ColorFromIndex(idx)).ToArray();

            Bitmap reducedImage = new Bitmap(imageWidth, imageHeight);

            int pixelIdx = 0;
            for(int y = 0; y < imageHeight; y++)
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    Color colorToDisplay = CalcDisplayColor(Color.FromArgb(pixels[pixelIdx], pixels[pixelIdx + 1], pixels[pixelIdx + 2]));
                    reducedImage.SetPixel(x, y, colorToDisplay);
                    pixelIdx += 3;
                }
            }
            return reducedImage;
        }

        private Color CalcDisplayColor(Color col)
        {
            Color displayCol = displayColors[0];
            int minDist = ColorsDistance2(displayColors[0], col);

            for (int i = 1; i < displayColors.Length && minDist > 0; i++)
            {
                int dist = ColorsDistance2(displayColors[i], col);
                if (dist < minDist)
                {
                    minDist = dist;
                    displayCol = displayColors[i];
                }
            }
            return displayCol;
        }

        private int ColorsDistance2(Color c1, Color c2)
        {
            int deltaR = c2.R - c1.R;
            int deltaG = c2.G - c1.G;
            int deltaB = c2.B - c1.B;

            return deltaR * deltaR + deltaG * deltaG + deltaB * deltaB;
        }

        private Color ColorFromIndex(int idx)
        {
            return Color.FromArgb(idx >> 16 & 0xFF, idx >> 8 & 0xFF, idx & 0xFF);
        }
    }

}
