using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GmmImageSegmentator.Utilities;

namespace GmmImageSegmentator.Filters
{
    public class EdgeDetectionFilter : IImageFilter
    {
        private readonly int[] _labels;
        private readonly int _width, _height;

        public EdgeDetectionFilter(int[] labels, int width, int height)
        {
            _labels = labels;
            _width = width;
            _height = height;
        }

        public BitmapImage Apply(BitmapImage source)
        {
            // Игнорируем source, генерируем новое изображение на основе меток
            byte[] pixelData = new byte[_width * _height * 3];
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int idx = y * _width + x;
                    int label = _labels[idx];
                    bool isEdge = false;
                    if (x > 0 && _labels[idx - 1] != label) isEdge = true;
                    else if (x < _width - 1 && _labels[idx + 1] != label) isEdge = true;
                    else if (y > 0 && _labels[idx - _width] != label) isEdge = true;
                    else if (y < _height - 1 && _labels[idx + _width] != label) isEdge = true;
                    byte val = isEdge ? (byte)255 : (byte)0;
                    pixelData[idx * 3] = val;
                    pixelData[idx * 3 + 1] = val;
                    pixelData[idx * 3 + 2] = val;
                }
            }
            var wb = new WriteableBitmap(_width, _height, 96, 96, PixelFormats.Rgb24, null);
            wb.WritePixels(new System.Windows.Int32Rect(0, 0, _width, _height), pixelData, _width * 3, 0);
            return ImageLoader.ConvertWriteableBitmapToBitmapImage(wb);
        }
    }
}