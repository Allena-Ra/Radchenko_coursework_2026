using GmmImageSegmentator.Filters.Interfaces;
using System.Collections.Generic;
using System.Windows.Media;

namespace GmmImageSegmentator.Filters
{
    /// <summary>
    /// Фильтр, инвертирующий цвета палитры (R = 255-R, G = 255-G, B = 255-B).
    /// </summary>
    public class InvertColorsFilter : IPaletteFilter
    {
        /// <summary>
        /// Возвращает новую палитру с инвертированными цветами.
        /// </summary>
        /// <param name="currentPalette">Исходная палитра.</param>
        /// <returns>Инвертированная палитра.</returns>
        public List<Color> Apply(List<Color> currentPalette)
        {
            var inverted = new List<Color>(currentPalette.Count);
            foreach (var c in currentPalette)
            {
                inverted.Add(Color.FromRgb((byte)(255 - c.R), (byte)(255 - c.G), (byte)(255 - c.B)));
            }
            return inverted;
        }
    }
}