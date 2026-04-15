using GmmImageSegmentator.Filters;
using GmmImageSegmentator.Utilities;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;

public class EdgeDetectionFilter : ImageFilter
{
    public override BitmapImage Apply(int[] labels, int width, int height, int k, List<MediaColor>? clusterMeanColors = null)
    {
        byte[] pixelData = new byte[width * height * 3];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                int label = labels[idx];
                bool isEdge = false;
                // Проверяем 4 соседей
                if (x > 0 && labels[idx - 1] != label) isEdge = true;
                else if (x < width - 1 && labels[idx + 1] != label) isEdge = true;
                else if (y > 0 && labels[idx - width] != label) isEdge = true;
                else if (y < height - 1 && labels[idx + width] != label) isEdge = true;
                byte val = isEdge ? (byte)255 : (byte)0;
                pixelData[idx * 3] = val;
                pixelData[idx * 3 + 1] = val;
                pixelData[idx * 3 + 2] = val;
            }
        }
        var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
        wb.WritePixels(new Int32Rect(0, 0, width, height), pixelData, width * 3, 0);
        return ImageLoader.ConvertWriteableBitmapToBitmapImage(wb);
    }
}