using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Media.Imaging;

namespace GmmImageSegmentator.Utilities
{
    /// <summary>
    /// Статический помощник для загрузки изображений из файлов и сохранения BitmapSource в файлы.
    /// </summary>
    public static class ImageFileHelper
    {
        /// <summary>
        /// Загружает изображение из файла, масштабирует его с заданным коэффициентом,
        /// возвращает массив нормализованных пикселей (RGB в диапазоне [0,1]) и
        /// готовый BitmapImage для отображения в WPF.
        /// </summary>
        /// <param name="filePath">Полный путь к файлу изображения.</param>
        /// <param name="scaleFactor">Коэффициент масштабирования (0.1 – 1.0).</param>
        /// <returns>
        /// Кортеж, содержащий:
        /// <list type="bullet">
        /// <item><description>width – ширина масштабированного изображения (в пикселях).</description></item>
        /// <item><description>height – высота масштабированного изображения.</description></item>
        /// <item><description>pixels – массив пикселей, каждый элемент – double[3] (R,G,B) нормализованные.</description></item>
        /// <item><description>displayImage – BitmapImage для привязки к Image.Source в WPF.</description></item>
        /// </list>
        /// </returns>
        public static (int width, int height, double[][] pixels, BitmapImage displayImage) Load(string filePath, double scaleFactor)
        {
            using (var original = new Bitmap(filePath))
            {
                // Вычисляем новые размеры с учётом коэффициента масштабирования
                int newWidth = Math.Max(1, (int)(original.Width * scaleFactor));
                int newHeight = Math.Max(1, (int)(original.Height * scaleFactor));

                // Создаём пустой Bitmap нужного размера
                var scaledBitmap = new Bitmap(newWidth, newHeight);
                using (var g = Graphics.FromImage(scaledBitmap))
                {
                    // Используем высококачественную интерполяцию для плавного масштабирования
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(original, 0, 0, newWidth, newHeight);
                }

                // Извлекаем пиксели из масштабированного изображения и нормализуем их в диапазон [0,1]
                int totalPixels = newWidth * newHeight;
                double[][] pixels = new double[totalPixels][];
                for (int y = 0; y < newHeight; y++)
                {
                    for (int x = 0; x < newWidth; x++)
                    {
                        System.Drawing.Color c = scaledBitmap.GetPixel(x, y);
                        pixels[y * newWidth + x] = new double[] { c.R / 255.0, c.G / 255.0, c.B / 255.0 };
                    }
                }

                var displayImage = ImageConverter.ConvertBitmapToBitmapImage(scaledBitmap);
                return (newWidth, newHeight, pixels, displayImage);
            }
        }

        /// <summary>
        /// Сохраняет BitmapSource (например, WriteableBitmap или BitmapImage) в файл PNG.
        /// </summary>
        /// <param name="image">Исходное изображение в формате BitmapSource.</param>
        /// <param name="filePath">Путь для сохранения (с расширением .png).</param>
        public static void SaveBitmapSource(BitmapSource image, string filePath)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using (var stream = new FileStream(filePath, FileMode.Create))
                encoder.Save(stream);
        }
    }
}