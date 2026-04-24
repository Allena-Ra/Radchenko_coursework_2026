using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GmmImageSegmentator.Filters.Interfaces;
using GmmImageSegmentator.Utilities;

namespace GmmImageSegmentator.Filters
{
    public class RecolorFilter : IImageFilter
    {
        private readonly int[] _labels;
        private readonly int _width, _height;
        private readonly Func<List<Color>> _getPalette;
        private readonly Action<List<Color>> _setPalette;

        public RecolorFilter(int[] labels, int width, int height, Func<List<Color>> getPalette, Action<List<Color>> setPalette)
        {
            _labels = labels;
            _width = width;
            _height = height;
            _getPalette = getPalette;
            _setPalette = setPalette;
        }

        public BitmapImage Apply(BitmapImage source)
        {
            var palette = _getPalette();
            return ImageLoader.CreateSegmentedImageFromColors(_labels, _width, _height, palette);
        }
    }
}