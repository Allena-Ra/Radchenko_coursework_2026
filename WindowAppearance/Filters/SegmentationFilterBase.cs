using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GmmImageSegmentator.Filters
{
    public abstract class SegmentationFilterBase : IImageFilter
    {
        protected int[] _labels;
        protected int _width, _height, _k;
        protected List<Color> _clusterMeanColors;

        public SegmentationFilterBase(int[] labels, int width, int height, int k, List<Color> clusterMeanColors)
        {
            _labels = labels;
            _width = width;
            _height = height;
            _k = k;
            _clusterMeanColors = clusterMeanColors;
        }

        public abstract BitmapImage Apply(BitmapImage source);
    }
}