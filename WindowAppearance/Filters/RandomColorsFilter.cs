using GmmImageSegmentator.Filters.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace GmmImageSegmentator.Filters
{
    /// <summary>
    /// Фильтр, присваивающий каждому кластеру случайный цвет.
    /// Количество цветов совпадает с размером текущей палитры.
    /// </summary>
    public class RandomColorsFilter : IPaletteFilter
    {
        /// <summary>
        /// Генерирует новую палитру со случайными цветами.
        /// </summary>
        /// <param name="currentPalette">Текущая палитра (используется только для определения количества цветов).</param>
        /// <returns>Палитра со случайными цветами.</returns>
        public List<Color> Apply(List<Color> currentPalette)
        {
            var rand = new Random();
            var randomColors = new List<Color>(currentPalette.Count);
            for (int i = 0; i < currentPalette.Count; i++)
            {
                randomColors.Add(Color.FromRgb((byte)rand.Next(256), (byte)rand.Next(256), (byte)rand.Next(256)));
            }
            return randomColors;
        }
    }
}