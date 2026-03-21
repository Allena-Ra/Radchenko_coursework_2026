using System.Drawing;

namespace ImageParser
{
   /// <summary>
   /// класс для загрузки изображения. разбивает на карту пикселей для последующей обработки
   /// </summary>
    public static class ImageLoader
    {
        
        /// <summary>
        /// Загружает изображение, разбивает на пиксели и нормализует получившиеся вектора
        /// </summary>
        /// <param name="filePath">путь к файлу, который нужнотзагрузить</param>
        /// <returns> кортеж из ширины, высоты и массив уже нормализованных пикселей </returns>
        public static (int width, int height, double[][] pixels) Load(string filePath)
        {
            using (var bitmap = new Bitmap(filePath))
            {
                int width = bitmap.Width;
                int height = bitmap.Height;
                int totalPixels = width * height;
                double[][] pixels = new double[totalPixels][];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color color = bitmap.GetPixel(x, y);
                        double[] vector = new double[3];
                        vector[0] = color.R / 255.0;   // R
                        vector[1] = color.G / 255.0;   // G
                        vector[2] = color.B / 255.0;   // B
                        pixels[y * width + x] = vector;
                    }
                }
                return (width, height, pixels);
            }
        }
    }
}