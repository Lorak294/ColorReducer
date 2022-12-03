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

        public ColorReducer(Bitmap baseImage)
        {
            imageWidth = baseImage.Width;
            imageHeight = baseImage.Height;

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

        protected int ColorsDistance2(Color c1, Color c2)
        {
            int deltaR = c2.R - c1.R;
            int deltaG = c2.G - c1.G;
            int deltaB = c2.B - c1.B;

            return deltaR * deltaR + deltaG * deltaG + deltaB * deltaB;
        }
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

        private Color ColorFromIndex(int idx)
        {
            return Color.FromArgb(idx >> 16 & 0xFF, idx >> 8 & 0xFF, idx & 0xFF);
        }
    }


    public class KMeansColorReducer : ColorReducer
    {
        private int epsilon;
        public KMeansColorReducer(Bitmap baseImage, int epsilon) : base(baseImage)
        {
            this.epsilon = epsilon;
        }
        public override Bitmap Reduce(int colorN)
        {
            Color[] centroids = new Color[colorN];
            Random random = new Random();
            int[] pixelToCentroid = new int[imageWidth*imageHeight];
            (int R, int G, int B)[] centroidSums = new (int R, int G, int B)[colorN];
            int[] centroidCount = new int[colorN];

            
            // choose random initial centroids
            for(int i=0;i<colorN;i++)
            {
                int rndPixelIdx = random.Next(imageWidth*imageHeight)*3;
                centroids[i] = Color.FromArgb(pixels[rndPixelIdx], pixels[rndPixelIdx + 1], pixels[rndPixelIdx + 2]);
            }


            bool centroidsChange = true;
            // kmeans algorithm loop
            while (centroidsChange)
            {
                // assign pixels to centorids
                for(int pixelIdx=0; pixelIdx<pixels.Length; pixelIdx+=3 )
                {
                    Color c = Color.FromArgb(pixels[pixelIdx], pixels[pixelIdx + 1], pixels[pixelIdx + 2]);

                    int newCentroidId = GetClosestCentroid(c, centroids);
                    pixelToCentroid[pixelIdx / 3] = newCentroidId;
                    centroidSums[newCentroidId] = (centroidSums[newCentroidId].R + c.R, centroidSums[newCentroidId].G + c.G, centroidSums[newCentroidId].B + c.B);
                    centroidCount[newCentroidId]++;
                }

                // calc new centorids
                centroidsChange = false;
                for(int i=0; i<colorN; i++)
                {
                    if (centroidCount[i] == 0)
                        continue;
                    Color averageColor = Color.FromArgb((centroidSums[i].R / centroidCount[i]), (centroidSums[i].G / centroidCount[i]), (centroidSums[i].B / centroidCount[i]));
                    if (Math.Abs(centroids[i].R - averageColor.R) > epsilon || Math.Abs(centroids[i].G - averageColor.G) > epsilon || Math.Abs(centroids[i].B - averageColor.B) > epsilon)
                    {
                        centroidsChange = true;
                        centroids[i] = averageColor;
                    }
                }
            }

            // compose reduced image
            Bitmap reducedImage = new Bitmap(imageWidth, imageHeight);
            int pixelId = 0;
            for(int y=0; y<imageHeight; y++)
            {
                for(int x=0; x<imageWidth; x++)
                {
                    reducedImage.SetPixel(x,y,centroids[pixelToCentroid[pixelId]]);
                    pixelId++;
                }
            }
            return reducedImage;
        }

        private int GetClosestCentroid(Color c, Color[] centroids)
        {
            double minDist = ColorsDistance2(c, centroids[0]);
            int centroId = 0;
            for (int i = 1; i < centroids.Length; i++)
            {
                var dist = ColorsDistance2(c, centroids[i]);

                if (dist < minDist)
                {
                    minDist = dist;
                    centroId = i;
                }
            }
            return centroId;
        }
    }
}
