using System.Windows;
using System.Windows.Media.Imaging;
using static GmmImageSegmentator.MainViewModel;

namespace GmmImageSegmentator
{
    public partial class ComparisonWindow : Window
    {
        public BitmapImage CustomImage { get; private set; }
        public BitmapImage AccordImage { get; private set; }
        public ComparisonResult SelectedResult { get; private set; }

        public ComparisonWindow(BitmapImage customImage, BitmapImage accordImage)
        {
            InitializeComponent();
            CustomImage = customImage;
            AccordImage = accordImage;
            CustomImageControl.Source = customImage;
            AccordImageControl.Source = accordImage;
        }

        private void UseCustom_Click(object sender, RoutedEventArgs e)
        {
            SelectedResult = ComparisonResult.Custom;
            DialogResult = true;
            Close();
        }

        private void UseAccord_Click(object sender, RoutedEventArgs e)
        {
            SelectedResult = ComparisonResult.Accord;
            DialogResult = true;
            Close();
        }
    }
}