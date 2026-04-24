using GmmImageSegmentator.Filters.Interfaces;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GmmImageSegmentator.Filters
{
    /// <summary>
    /// Фильтр, выделяющий границы между кластерами на сегментированном изображении.
    /// Пиксель считается граничным, если хотя бы один из четырёх соседей (слева,
    /// справа, сверху, снизу) принадлежит другому кластеру.
    /// Результат — чёрно-белое изображение, где белые пиксели обозначают границы.
    /// </summary>
    public class EdgeDetectionFilter : IImageFilter
    {
        private readonly int[] _labels;
        private readonly int _width, _height;

        /// <summary>
        /// Инициализирует фильтр границ.
        /// </summary>
        /// <param name="labels">Метки кластеров для каждого пикселя (одномерный массив).</param>
        /// <param name="width">Ширина изображения.</param>
        /// <param name="height">Высота изображения.</param>
        public EdgeDetectionFilter(int[] labels, int width, int height)
        {
            _labels = labels;
            _width = width;
            _height = height;
        }

        /// <summary>
        /// Создаёт изображение границ на основе информации о метках.
        /// Параметр source игнорируется.
        /// </summary>
        /// <param name="source">Не используется.</param>
        /// <returns>Чёрно-белое изображение с выделенными границами.</returns>
        public BitmapImage Apply(BitmapImage source)
        {
            byte[] pixelData = new byte[_width * _height * 3];
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    int idx = y * _width + x;
                    int label = _labels[idx];
                    bool isEdge = false;

                    // Проверяем всех четырёх соседей. Если хоть один отличается — это край.
                    if ((x > 0 && _labels[idx - 1] != label) ||
                        (x < _width - 1 && _labels[idx + 1] != label) ||
                        (y > 0 && _labels[idx - _width] != label) ||
                        (y < _height - 1 && _labels[idx + _width] != label))
                    {
                        isEdge = true;
                    }

                    byte val = isEdge ? (byte)255 : (byte)0;
                    pixelData[idx * 3] = val;
                    pixelData[idx * 3 + 1] = val;
                    pixelData[idx * 3 + 2] = val;
                }
            }

            var wb = new WriteableBitmap(_width, _height, 96, 96, PixelFormats.Rgb24, null);
            wb.WritePixels(new System.Windows.Int32Rect(0, 0, _width, _height), pixelData, _width * 3, 0);
            return Utilities.ImageLoader.ConvertWriteableBitmapToBitmapImage(wb);
        }
    }
}