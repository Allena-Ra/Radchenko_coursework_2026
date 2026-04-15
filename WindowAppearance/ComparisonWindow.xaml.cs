using System.Windows;
using System.Windows.Media.Imaging;

namespace GmmImageSegmentator
{
    public partial class ComparisonWindow : Window
    {
        public ComparisonWindow(BitmapImage customImage, BitmapImage accordImage)
        {
            InitializeComponent();
            CustomImage.Source = customImage;
            AccordImage.Source = accordImage;
        }
    }
}