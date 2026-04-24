using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GmmImageSegmentator.Utilities
{
    /// <summary>
    /// Фасад, объединяющий операции загрузки, сохранения, конвертации и построения
    /// сегментированных изображений. Делегирует работу специализированным классам:
    /// <see cref="ImageFileHelper"/>, <see cref="ImageConverter"/>,
    /// <see cref="SegmentedImageBuilder"/>.
    /// </summary>
    public static class ImageLoader
    {
        /// <inheritdoc cref="ImageFileHelper.Load"/>
        public static (int width, int height, double[][] pixels, BitmapImage displayImage)
            Load(string filePath, double scaleFactor)
            => ImageFileHelper.Load(filePath, scaleFactor);

        /// <inheritdoc cref="ImageFileHelper.SaveBitmapSource"/>
        public static void SaveBitmapSource(BitmapSource image, string filePath)
            => ImageFileHelper.SaveBitmapSource(image, filePath);

        /// <inheritdoc cref="ImageConverter.ConvertBitmapToBitmapImage"/>
        internal static BitmapImage ConvertBitmapToBitmapImage(System.Drawing.Bitmap bitmap)
            => ImageConverter.ConvertBitmapToBitmapImage(bitmap);

        /// <inheritdoc cref="ImageConverter.ConvertWriteableBitmapToBitmapImage"/>
        public static BitmapImage ConvertWriteableBitmapToBitmapImage(WriteableBitmap wb)
            => ImageConverter.ConvertWriteableBitmapToBitmapImage(wb);

        /// <inheritdoc cref="ImageConverter.GetPixels"/>
        public static byte[] GetPixels(BitmapImage source)
            => ImageConverter.GetPixels(source);

        /// <inheritdoc cref="SegmentedImageBuilder.CreateSegmentedImage"/>
        public static BitmapImage CreateSegmentedImage(int[] labels, double[][] pixels,
            int width, int height, int k)
            => SegmentedImageBuilder.CreateSegmentedImage(labels, pixels, width, height, k);

        /// <inheritdoc cref="SegmentedImageBuilder.CreateSegmentedImageFromColors"/>
        public static BitmapImage CreateSegmentedImageFromColors(int[] labels, int width, int height,
            List<Color> clusterColors)
            => SegmentedImageBuilder.CreateSegmentedImageFromColors(labels, width, height, clusterColors);
    }
}