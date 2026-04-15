using GmmImageSegmentator.Utilities;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;

namespace GmmImageSegmentator.Filters
{
    public class RecolorFilter : ImageFilter
    {
        public List<MediaColor> NewColors { get; set; } = new();

        public override BitmapImage Apply(int[] labels, int width, int height, int k, List<MediaColor>? clusterMeanColors = null)
        {
            if (NewColors.Count != k)
            {
                // Если цветов не хватает, дополняем черным
                while (NewColors.Count < k) NewColors.Add(Colors.Black);
            }
            return ImageLoader.CreateSegmentedImageFromColors(labels, width, height, NewColors);
        }
    }
}