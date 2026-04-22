using System.Windows;
using System.Windows.Media;

namespace GmmImageSegmentator
{
    public partial class ColorPickerDialog : Window
    {
        public Color SelectedColor { get; private set; }

        public ColorPickerDialog(Color initialColor)
        {
            InitializeComponent();
            RSlider.Value = initialColor.R;
            GSlider.Value = initialColor.G;
            BSlider.Value = initialColor.B;
            UpdatePreview();
            RSlider.ValueChanged += (s, e) => UpdatePreview();
            GSlider.ValueChanged += (s, e) => UpdatePreview();
            BSlider.ValueChanged += (s, e) => UpdatePreview();
        }

        private void UpdatePreview()
        {
            ColorPreview.Fill = new SolidColorBrush(Color.FromRgb((byte)RSlider.Value, (byte)GSlider.Value, (byte)BSlider.Value));
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            SelectedColor = Color.FromRgb((byte)RSlider.Value, (byte)GSlider.Value, (byte)BSlider.Value);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}