using System.Windows.Media.Imaging;

namespace GmmImageSegmentator.Filters.Interfaces
{
    /// <summary>
    /// Фильтр, преобразующий исходное растровое изображение в новое.
    /// Не зависит от палитры 
    /// </summary>
    public interface IImageFilter
    {
        /// <summary>
        /// Применяет фильтр к изображению.
        /// </summary>
        /// <param name="source">Исходное изображение. Не может быть null.</param>
        /// <returns>Новое изображение после обработки.</returns>
        BitmapImage Apply(BitmapImage source);
    }
}