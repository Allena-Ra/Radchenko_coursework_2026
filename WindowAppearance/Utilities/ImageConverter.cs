using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace GmmImageSegmentator.Utilities
{
    /// <summary>
    /// Статический класс для преобразований между Windows Forms и WPF изображениями,
    /// а также для извлечения пиксельных данных.
    /// </summary>
    public static class ImageConverter
    {
        /// <summary>
        /// Преобразует System.Drawing.Bitmap в BitmapImage.
        /// </summary>
        public static BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
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
        /// Преобразует WriteableBitmap в BitmapImage.
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

        /// <summary>
        /// Извлекает массив пикселей (RGB) из BitmapImage.
        /// </summary>
        public static byte[] GetPixels(BitmapImage source)
        {
            var wb = new WriteableBitmap(source);
            int stride = wb.PixelWidth * (wb.Format.BitsPerPixel / 8);
            byte[] pixels = new byte[wb.PixelHeight * stride];
            wb.CopyPixels(pixels, stride, 0);
            return pixels;
        }
    }
}