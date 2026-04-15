using GmmImageSegmentator.Filters;
using GmmImageSegmentator.Utilities;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GmmImageSegmentator
{
    public partial class FilterWindow : Window
    {
        private readonly int[] _labels;
        private readonly int _width, _height, _k;
        private readonly List<Color> _clusterMeanColors;
        private BitmapImage _currentFilteredImage;
        private RecolorFilter _recolorFilter;

        public FilterWindow(int[] labels, int width, int height, int k, List<Color> clusterMeanColors)
        {
            InitializeComponent();
            _labels = labels;
            _width = width;
            _height = height;
            _k = k;
            _clusterMeanColors = clusterMeanColors;
            LoadFilters();
        }

        private void LoadFilters()
        {
            _recolorFilter = new RecolorFilter();
            // Инициализируем _recolorFilter.NewColors средними цветами
            _recolorFilter.NewColors.Clear();
            _recolorFilter.NewColors.AddRange(_clusterMeanColors);

            var filters = new List<FilterItem>
    {
        new FilterItem { Name = "Перекраска кластеров", Filter = _recolorFilter, SettingsControl = CreateRecolorSettings() },
        new FilterItem { Name = "Инверсия цветов", Filter = new InvertColorsFilter(), SettingsControl = null },
        new FilterItem { Name = "Границы кластеров", Filter = new EdgeDetectionFilter(), SettingsControl = null }
    };
            FiltersListBox.ItemsSource = filters;
        }

        private UIElement CreateRecolorSettings()
        {
            var panel = new StackPanel();
            for (int i = 0; i < _k; i++)
            {
                int cluster = i;
                var color = _recolorFilter.NewColors[cluster];
                var rect = new Rectangle { Width = 20, Height = 20, Fill = new SolidColorBrush(color), Stroke = Brushes.Black };
                rect.MouseLeftButtonUp += (s, e) =>
                {
                    var dialog = new ColorPickerDialog(_recolorFilter.NewColors[cluster]);
                    if (dialog.ShowDialog() == true)
                    {
                        _recolorFilter.NewColors[cluster] = dialog.SelectedColor;
                        rect.Fill = new SolidColorBrush(dialog.SelectedColor);
                        // Не применяем фильтр автоматически – ждём кнопку "Применить"
                    }
                };
                var label = new TextBlock { Text = $"Кластер {i}", Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
                var colorPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
                colorPanel.Children.Add(rect);
                colorPanel.Children.Add(label);
                panel.Children.Add(colorPanel);
            }
            return panel;
        }


        private void FiltersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterSettingsPanel.Children.Clear();
            if (FiltersListBox.SelectedItem is FilterItem item && item.SettingsControl != null)
                FilterSettingsPanel.Children.Add(item.SettingsControl);
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            if (FiltersListBox.SelectedItem is FilterItem item)
            {
                _currentFilteredImage = item.Filter.Apply(_labels, _width, _height, _k, _clusterMeanColors);
                FilteredImage.Source = _currentFilteredImage;
            }
        }

        private void SaveResult_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFilteredImage == null) return;
            var dialog = new SaveFileDialog { Filter = "PNG Image|*.png", DefaultExt = "png" };
            if (dialog.ShowDialog() == true)
                ImageLoader.SaveBitmapSource(_currentFilteredImage, dialog.FileName);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }

    public class FilterItem
    {
        public string Name { get; set; }
        public ImageFilter Filter { get; set; }
        public UIElement SettingsControl { get; set; }
    }
}