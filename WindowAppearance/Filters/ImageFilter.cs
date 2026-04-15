using System.Windows.Media.Imaging;

namespace GmmImageSegmentator.Filters
{
    /// <summary>
    /// Базовый класс для фильтров, применяемых к сегментированному изображению.
    /// </summary>
    public abstract class ImageFilter
    {
        /// <summary>
        /// Применяет фильтр к данным сегментации.
        /// </summary>
        /// <param name="labels">Метки кластеров.</param>
        /// <param name="width">Ширина изображения.</param>
        /// <param name="height">Высота изображения.</param>
        /// <param name="k">Количество кластеров.</param>
        /// <param name="clusterMeanColors">Средние цвета кластеров (опционально).</param>
        /// <returns>BitmapImage после применения фильтра.</returns>
        public abstract BitmapImage Apply(int[] labels, int width, int height, int k, List<System.Windows.Media.Color>? clusterMeanColors = null);
    }
}