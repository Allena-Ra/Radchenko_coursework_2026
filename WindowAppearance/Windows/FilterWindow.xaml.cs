using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using GmmImageSegmentator.Filters;
using GmmImageSegmentator.Utilities;
using System.Windows.Shapes;

namespace GmmImageSegmentator
{
    public partial class FilterWindow : Window
    {
        private readonly int[] _labels;
        private readonly int _width, _height, _k;
        private readonly List<Color> _clusterMeanColors;
        private List<Color> _currentPalette;
        private BitmapImage _currentImage;
        private Stack<List<Color>> _undoStack = new();
        private Stack<List<Color>> _redoStack = new();

        public FilterWindow(int[] labels, int width, int height, int k, List<Color> clusterMeanColors)
        {
            InitializeComponent();
            _labels = labels;
            _width = width;
            _height = height;
            _k = k;
            _clusterMeanColors = clusterMeanColors;
            _currentPalette = new List<Color>(clusterMeanColors);
            _undoStack.Push(new List<Color>(_currentPalette));
            _currentImage = ImageLoader.CreateSegmentedImageFromColors(_labels, _width, _height, _currentPalette);
            FilteredImage.Source = _currentImage;
            LoadFilters();
            UpdateUndoRedoButtons();
        }

        private void LoadFilters()
        {
            var invert = new InvertColorsFilter(_labels, _width, _height, () => _currentPalette, p => _currentPalette = p);
            var random = new RandomColorsFilter(_labels, _width, _height, () => _currentPalette, p => _currentPalette = p);
            var edge = new EdgeDetectionFilter(_labels, _width, _height);

            var filters = new List<FilterItem>
            {
                new FilterItem { Name = "Перекраска кластеров", Filter = null, IsColorFilter = true },
                new FilterItem { Name = "Инверсия цветов", Filter = invert, IsColorFilter = true },
                new FilterItem { Name = "Случайные цвета", Filter = random, IsColorFilter = true },
                new FilterItem { Name = "Границы кластеров", Filter = edge, IsColorFilter = false }
            };
            FiltersListBox.ItemsSource = filters;
        }

        private UIElement CreateRecolorSettings()
        {
            var panel = new StackPanel();
            for (int i = 0; i < _k; i++)
            {
                int cluster = i;
                var rect = new Rectangle { Width = 20, Height = 20, Fill = new SolidColorBrush(_currentPalette[cluster]), Stroke = Brushes.Black };
                rect.MouseLeftButtonUp += (s, e) =>
                {
                    var dialog = new ColorPickerDialog(_currentPalette[cluster]);
                    if (dialog.ShowDialog() == true)
                    {
                        PushUndo();
                        _currentPalette[cluster] = dialog.SelectedColor;
                        RefreshImage();
                        UpdateUndoRedoButtons();
                    }
                };
                var label = new TextBlock { Text = $"Кластер {cluster}", Margin = new Thickness(5, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
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
            if (FiltersListBox.SelectedItem is FilterItem item && item.Name == "Перекраска кластеров")
            {
                FilterSettingsPanel.Children.Add(CreateRecolorSettings());
            }
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            if (FiltersListBox.SelectedItem is FilterItem item)
            {
                if (item.IsColorFilter && item.Filter != null)
                {
                    PushUndo();
                    item.Filter.Apply(null); // обновляет _currentPalette
                    RefreshImage();
                    UpdateUndoRedoButtons();
                }
                else if (!item.IsColorFilter && item.Filter != null)
                {
                    // Границы – не меняют палитру, но мы можем сохранить текущее изображение в историю (опционально)
                    // Для простоты не добавляем в отмену, так как это отдельный режим.
                    _currentImage = item.Filter.Apply(null);
                    FilteredImage.Source = _currentImage;
                }
            }
        }

        private void RefreshImage()
        {
            _currentImage = ImageLoader.CreateSegmentedImageFromColors(_labels, _width, _height, _currentPalette);
            FilteredImage.Source = _currentImage;
            // Обновляем панель перекраски, если она открыта
            if (FiltersListBox.SelectedItem is FilterItem selected && selected.Name == "Перекраска кластеров")
            {
                FilterSettingsPanel.Children.Clear();
                FilterSettingsPanel.Children.Add(CreateRecolorSettings());
            }
        }

        private void PushUndo()
        {
            _undoStack.Push(new List<Color>(_currentPalette));
            _redoStack.Clear();
        }

        private void Undo()
        {
            if (_undoStack.Count > 0)
            {
                _redoStack.Push(new List<Color>(_currentPalette));
                _currentPalette = _undoStack.Pop();
                RefreshImage();
                UpdateUndoRedoButtons();
            }
        }

        private void Redo()
        {
            if (_redoStack.Count > 0)
            {
                _undoStack.Push(new List<Color>(_currentPalette));
                _currentPalette = _redoStack.Pop();
                RefreshImage();
                UpdateUndoRedoButtons();
            }
        }

        private void UpdateUndoRedoButtons()
        {
            UndoButton.IsEnabled = _undoStack.Count > 1; // сохраняем начальное состояние
            RedoButton.IsEnabled = _redoStack.Count > 0;
        }

        private void Undo_Click(object sender, RoutedEventArgs e) => Undo();
        private void Redo_Click(object sender, RoutedEventArgs e) => Redo();

        private void SaveResult_Click(object sender, RoutedEventArgs e)
        {
            if (_currentImage != null)
            {
                var dialog = new SaveFileDialog { Filter = "PNG Image|*.png", DefaultExt = "png" };
                if (dialog.ShowDialog() == true)
                    ImageLoader.SaveBitmapSource(_currentImage, dialog.FileName);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }

    public class FilterItem
    {
        public string Name { get; set; }
        public IImageFilter Filter { get; set; }
        public bool IsColorFilter { get; set; }
    }
}