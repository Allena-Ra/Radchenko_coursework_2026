using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GmmImageSegmentator.Utilities;

namespace GmmImageSegmentator.Filters
{
    public class RandomColorsFilter : IImageFilter
    {
        private readonly int[] _labels;
        private readonly int _width, _height;
        private readonly Func<List<Color>> _getPalette;
        private readonly Action<List<Color>> _setPalette;

        public RandomColorsFilter(int[] labels, int width, int height, Func<List<Color>> getPalette, Action<List<Color>> setPalette)
        {
            _labels = labels;
            _width = width;
            _height = height;
            _getPalette = getPalette;
            _setPalette = setPalette;
        }

        public BitmapImage Apply(BitmapImage source)
        {
            var rand = new Random();
            var randomColors = new List<Color>();
            for (int i = 0; i < _getPalette().Count; i++)
                randomColors.Add(Color.FromRgb((byte)rand.Next(256), (byte)rand.Next(256), (byte)rand.Next(256)));
            _setPalette(randomColors);
            return ImageLoader.CreateSegmentedImageFromColors(_labels, _width, _height, randomColors);
        }
    }
}