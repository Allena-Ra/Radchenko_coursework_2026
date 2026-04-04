using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ImageParser
{
    /// <summary>
    /// Класс для загрузки изображения. Разбивает на карту пикселей для последующей обработки
    /// </summary>
    public static class ImageLoader
    {
        /// <summary>
        /// Загружает изображение, масштабирует его, разбивает на пиксели и нормализует получившиеся вектора
        /// </summary>
        /// <param name="filePath">Путь к файлу, который нужно загрузить</param>
        /// <param name="scaleFactor">Коэффициент масштабирования (0.1 - 1.0)</param>
        /// <returns>Кортеж из ширины, высоты и массива нормализованных пикселей</returns>
        public static (int width, int height, double[][] pixels) Load(string filePath, double scaleFactor)
        {
            using (var originalBitmap = new Bitmap(filePath))
            {
                // Вычисляем новые размеры
                int newWidth = Math.Max(1, (int)(originalBitmap.Width * scaleFactor));
                int newHeight = Math.Max(1, (int)(originalBitmap.Height * scaleFactor));

                // Создаём масштабированную копию изображения
                var scaledBitmap = new Bitmap(newWidth, newHeight);
                using (var graphics = Graphics.FromImage(scaledBitmap))
                {
                    // Настройка высококачественного масштабирования
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(originalBitmap, 0, 0, newWidth, newHeight);
                }

                int totalPixels = newWidth * newHeight;
                double[][] pixels = new double[totalPixels][];

                // Извлекаем пиксели из масштабированного изображения
                for (int y = 0; y < newHeight; y++)
                {
                    for (int x = 0; x < newWidth; x++)
                    {
                        Color color = scaledBitmap.GetPixel(x, y);
                        pixels[y * newWidth + x] = new double[]
                        {
                            color.R / 255.0,
                            color.G / 255.0,
                            color.B / 255.0
                        };
                    }
                }

                return (newWidth, newHeight, pixels);
            }
        }
    }
}