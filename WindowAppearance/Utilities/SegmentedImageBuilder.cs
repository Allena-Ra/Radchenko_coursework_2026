
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;

namespace GmmImageSegmentator.Utilities
{
    /// <summary>
    /// Статический класс для создания сегментированных изображений на основе меток кластеров.
    /// </summary>
    public static class SegmentedImageBuilder
    {
        /// <summary>
        /// Создаёт сегментированное изображение на основе меток кластеров.
        /// Каждый пиксель заменяется средним цветом его кластера.
        /// </summary>
        /// <param name="labels">Массив меток кластеров для каждого пикселя (длина = ширина * высота).</param>
        /// <param name="pixels">Исходный массив нормализованных пикселей (double[][3]).</param>
        /// <param name="width">Ширина изображения в пикселях.</param>
        /// <param name="height">Высота изображения.</param>
        /// <param name="k">Количество кластеров (компонент смеси).</param>
        /// <returns>BitmapImage – результат сегментации для отображения в WPF.</returns>
        public static BitmapImage CreateSegmentedImage(int[] labels, double[][] pixels, int width, int height, int k)
        {
            // Шаг 1: вычисляем центры кластеров (средние значения R,G,B для каждого кластера)
            double[][] centers = new double[k][];
            int[] counts = new int[k];
            for (int i = 0; i < k; i++) centers[i] = new double[3];

            for (int i = 0; i < labels.Length; i++)
            {
                int cluster = labels[i];
                counts[cluster]++;
                for (int c = 0; c < 3; c++)
                    centers[cluster][c] += pixels[i][c];
            }
            for (int i = 0; i < k; i++)
            {
                if (counts[i] > 0)
                {
                    for (int c = 0; c < 3; c++)
                        centers[i][c] /= counts[i];
                }
            }

            // Шаг 2: формируем массив байтов (RGB) для всех пикселей, заменяя цвет на цвет кластера
            byte[] pixelData = new byte[width * height * 3];
            int idx = 0;
            for (int i = 0; i < labels.Length; i++)
            {
                int cluster = labels[i];
                pixelData[idx++] = (byte)(centers[cluster][0] * 255);
                pixelData[idx++] = (byte)(centers[cluster][1] * 255);
                pixelData[idx++] = (byte)(centers[cluster][2] * 255);
            }

            return BuildBitmapFromPixelData(pixelData, width, height);
        }

        /// <summary>
        /// 
        /// </summary>
        public static BitmapImage CreateSegmentedImageFromColors(int[] labels, int width, int height, List<Color> clusterColors)
        {
            if (width <= 0 || height <= 0) throw new ArgumentException("Width and height must be positive.");
            byte[] pixelData = new byte[width * height * 3];
            int idx = 0;
            for (int i = 0; i < labels.Length; i++)
            {
                int cluster = labels[i];
                var c = clusterColors[cluster];
                pixelData[idx++] = c.R;
                pixelData[idx++] = c.G;
                pixelData[idx++] = c.B;
            }

            return BuildBitmapFromPixelData(pixelData, width, height);
        }

        private static BitmapImage BuildBitmapFromPixelData(byte[] pixelData, int width, int height)
        {
            // создаём WriteableBitmap и заполняем его данными
            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
            wb.WritePixels(new Int32Rect(0, 0, width, height), pixelData, width * 3, 0);
            // конвертируем WriteableBitmap в BitmapImage для удобной привязки
            return ImageConverter.ConvertWriteableBitmapToBitmapImage(wb);
        }
    }
}