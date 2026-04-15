using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GmmImageSegmentator.Utilities
{
    /// <summary>
    /// Статический класс для работы с изображениями: загрузка, масштабирование,
    /// преобразование в пиксельные массивы, создание сегментированного изображения,
    /// сохранение результата.
    /// </summary>
    public static class ImageLoader
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

                // Конвертируем масштабированный Bitmap в BitmapImage для WPF
                var displayImage = ConvertBitmapToBitmapImage(scaledBitmap);
                return (newWidth, newHeight, pixels, displayImage);
            }
        }

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

            // Шаг 3: создаём WriteableBitmap и заполняем его данными
            var wb = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Rgb24, null);
            wb.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), pixelData, width * 3, 0);

            // Шаг 4: конвертируем WriteableBitmap в BitmapImage для удобной привязки
            return ConvertWriteableBitmapToBitmapImage(wb);
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

        // ----- Вспомогательные приватные методы -----

        /// <summary>
        /// Конвертирует System.Drawing.Bitmap в System.Windows.Media.Imaging.BitmapImage.
        /// </summary>
        private static BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                var img = new BitmapImage();
                img.BeginInit();
                img.StreamSource = ms;
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                img.Freeze(); // замораживаем для безопасного использования из разных потоков
                return img;
            }
        }
        /// <summary>
        /// Создаёт сегментированное изображение на основе заданных цветов для каждого кластера.
        /// </summary>
        public static BitmapImage CreateSegmentedImageFromColors(int[] labels, int width, int height, List<System.Windows.Media.Color> clusterColors)
        {
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
            var wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
            wb.WritePixels(new Int32Rect(0, 0, width, height), pixelData, width * 3, 0);
            return ConvertWriteableBitmapToBitmapImage(wb);
        }

        /// <summary>
        /// Конвертирует WriteableBitmap в BitmapImage (публичная версия).
        /// </summary>
        public static BitmapImage ConvertWriteableBitmapToBitmapImage(WriteableBitmap wb)
        {
            using (var ms = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(wb));
                encoder.Save(ms);
                ms.Position = 0;
                var img = new BitmapImage();
                img.BeginInit();
                img.StreamSource = ms;
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                img.Freeze();
                return img;
            }
        }
    }
}