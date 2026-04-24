using System.Collections.Generic;
using System.Windows.Media;

namespace GmmImageSegmentator.Filters.Interfaces
{
    /// <summary>
    /// Фильтр, изменяющий только палитру цветов, назначенных кластерам.
    /// Реализации не работают с пикселями напрямую, а лишь генерируют новую
    /// последовательность цветов, после чего изображение перестраивается
    /// из имеющихся меток.
    /// </summary>
    public interface IPaletteFilter
    {
        /// <summary>
        /// Создаёт новую палитру на основе текущей.
        /// </summary>
        /// <param name="currentPalette">Текущая палитра (не модифицируется).</param>
        /// <returns>Новая палитра, где каждому кластеру соответствует цвет.</returns>
        List<Color> Apply(List<Color> currentPalette);
    }
}