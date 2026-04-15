using System.Windows.Media.Imaging;

namespace GmmImageSegmentator.Filters
{
    /// <summary>
    /// Интерфейс для всех фильтров.
    /// </summary>
    public interface IImageFilter
    {
        /// <summary>
        /// Применяет фильтр к исходному изображению.
        /// </summary>
        /// <param name="source">Входное изображение (может быть null для фильтров, генерирующих изображение из меток).</param>
        /// <returns>Результирующее изображение.</returns>
        BitmapImage Apply(BitmapImage source);
    }
}