using GmmImageSegmentator.Filters;
using GmmImageSegmentator.Utilities;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;

public class InvertColorsFilter : ImageFilter
{
    public override BitmapImage Apply(int[] labels, int width, int height, int k, List<MediaColor>? clusterMeanColors = null)
    {
        if (clusterMeanColors == null) throw new ArgumentNullException(nameof(clusterMeanColors));
        var inverted = new List<MediaColor>();
        foreach (var c in clusterMeanColors)
            inverted.Add(MediaColor.FromRgb((byte)(255 - c.R), (byte)(255 - c.G), (byte)(255 - c.B)));
        return ImageLoader.CreateSegmentedImageFromColors(labels, width, height, inverted);
    }
}