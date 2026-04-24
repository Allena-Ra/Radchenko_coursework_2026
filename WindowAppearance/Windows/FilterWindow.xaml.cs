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
using GmmImageSegmentator.Filters.Interfaces;

namespace GmmImageSegmentator
{
    /// <summary>
    /// Окно для применения фильтров к сегментированному изображению.
    /// Поддерживает работу с палитрой, отмену и повтор действий, а также сохранение результата.
    /// </summary>
    public partial class FilterWindow : Window
    {
        private readonly int[] _labels;
        private readonly int _width, _height, _k;
        private readonly List<Color> _clusterMeanColors; // исходные средние цвета кластеров (не меняются)
        private List<Color> _currentPalette;
        private BitmapImage _currentImage;

        private readonly Stack<(BitmapImage Image, List<Color> Palette)> _undoStack = new();
        private readonly Stack<(BitmapImage Image, List<Color> Palette)> _redoStack = new();

        /// <summary>
        /// Создаёт окно фильтров.
        /// </summary>
        /// <param name="labels">Метки кластеров пикселей.</param>
        /// <param name="width">Ширина изображения.</param>
        /// <param name="height">Высота изображения.</param>
        /// <param name="k">Количество кластеров.</param>
        /// <param name="clusterMeanColors">Исходные средние цвета кластеров (для начальной палитры).</param>
        public FilterWindow(int[] labels, int width, int height, int k, List<Color> clusterMeanColors)
        {
            InitializeComponent();
            _labels = labels;
            _width = width;
            _height = height;
            _k = k;
            _clusterMeanColors = new List<Color>(clusterMeanColors);
            _currentPalette = new List<Color>(clusterMeanColors);
            _currentImage = ImageLoader.CreateSegmentedImageFromColors(_labels, _width, _height, _currentPalette);
            FilteredImage.Source = _currentImage;
            PushUndoState(); // начальное состояние
            LoadFilters();
            UpdateUndoRedoButtons();
        }

        /// <summary>
        /// Заполняет список доступных фильтров.
        /// </summary>
        private void LoadFilters()
        {
            var filters = new List<FilterItem>
            {
                new FilterItem
                {
                    Name = "Перекраска кластеров",
                    PaletteFilter = null,  // обрабатывается отдельно через панель
                    IsColorFilter = true,
                    HideApplyButton = true
                },
                new FilterItem
                {
                    Name = "Инверсия цветов",
                    PaletteFilter = new InvertColorsFilter(),
                    IsColorFilter = true,
                    HideApplyButton = false
                },
                new FilterItem
                {
                    Name = "Случайные цвета",
                    PaletteFilter = new RandomColorsFilter(),
                    IsColorFilter = true,
                    HideApplyButton = false
                },
                new FilterItem
                {
                    Name = "Границы кластеров",
                    ImageFilter = new EdgeDetectionFilter(_labels, _width, _height),
                    IsColorFilter = false,
                    HideApplyButton = false
                }
            };
            FiltersListBox.ItemsSource = filters;
        }

        /// <summary>
        /// Создаёт панель с возможностью выбора цвета для каждого кластера.
        /// </summary>
        private UIElement CreateRecolorSettings()
        {
            var panel = new StackPanel();
            for (int i = 0; i < _k; i++)
            {
                int cluster = i;
                var rect = new Rectangle
                {
                    Width = 20,
                    Height = 20,
                    Fill = new SolidColorBrush(_currentPalette[cluster]),
                    Stroke = Brushes.Black
                };
                rect.MouseLeftButtonUp += (s, e) =>
                {
                    var dialog = new ColorPickerDialog(_currentPalette[cluster]);
                    if (dialog.ShowDialog() == true)
                    {
                        PushUndoState();
                        _currentPalette[cluster] = dialog.SelectedColor;
                        RefreshAll();
                        UpdateUndoRedoButtons();
                    }
                };

                var label = new TextBlock
                {
                    Text = $"Кластер {cluster}",
                    Margin = new Thickness(5, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                var colorPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 2, 0, 2)
                };
                colorPanel.Children.Add(rect);
                colorPanel.Children.Add(label);
                panel.Children.Add(colorPanel);
            }
            return panel;
        }

        /// <summary>
        /// При выборе фильтра обновляет панель настроек и видимость кнопки «Применить».
        /// </summary>
        private void FiltersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterSettingsPanel.Children.Clear();
            if (FiltersListBox.SelectedItem is FilterItem item)
            {
                ApplyButton.Visibility = item.HideApplyButton ? Visibility.Collapsed : Visibility.Visible;
                if (item.Name == "Перекраска кластеров")
                {
                    FilterSettingsPanel.Children.Add(CreateRecolorSettings());
                }
            }
        }

        /// <summary>
        /// Обрабатывает нажатие кнопки «Применить».
        /// Для цветовых фильтров вызывает <see cref="IPaletteFilter.Apply"/> и перестраивает изображение.
        /// Для фильтров изображений просто вызывает <see cref="IImageFilter.Apply"/>.
        /// </summary>
        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            if (FiltersListBox.SelectedItem is not FilterItem item || item.HideApplyButton)
                return;

            PushUndoState(); // сохраняем состояние перед изменением

            if (item.IsColorFilter && item.PaletteFilter != null)
            {
                // Цветовой фильтр: получаем новую палитру и перестраиваем изображение
                _currentPalette = item.PaletteFilter.Apply(new List<Color>(_currentPalette));
                RefreshAll();
            }
            else if (!item.IsColorFilter && item.ImageFilter != null)
            {
                // Фильтр изображения: получаем новое изображение и обновляем превью
                _currentImage = item.ImageFilter.Apply(_currentImage);
                FilteredImage.Source = _currentImage;
            }

            UpdateUndoRedoButtons();
        }

        /// <summary>
        /// Перестраивает изображение из текущей палитры и обновляет панель перекраски,
        /// если она сейчас активна. Вызывается после любого изменения палитры.
        /// </summary>
        private void RefreshAll()
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

        /// <summary>
        /// Сохраняет текущее состояние в стек undo.
        /// </summary>
        private void PushUndoState()
        {
            _undoStack.Push((_currentImage, new List<Color>(_currentPalette)));
            _redoStack.Clear(); // любое новое действие сбрасывает redo
        }

        /// <summary>
        /// Выполняет перемещение состояния между двумя стеками (undo/redo).
        /// </summary>
        /// <param name="from">Стек, откуда берём состояние.</param>
        /// <param name="to">Стек, куда сохраняем текущее состояние.</param>
        private void MoveHistory(Stack<(BitmapImage, List<Color>)> from,
                                  Stack<(BitmapImage, List<Color>)> to)
        {
            if (from.Count > (from == _undoStack ? 1 : 0)) // для undo нужно хотя бы два состояния
            {
                to.Push((_currentImage, new List<Color>(_currentPalette)));
                var prev = from.Pop();
                _currentImage = prev.Item1;
                _currentPalette = prev.Item2;
                FilteredImage.Source = _currentImage;
                if (FiltersListBox.SelectedItem is FilterItem selected && selected.Name == "Перекраска кластеров")
                {
                    FilterSettingsPanel.Children.Clear();
                    FilterSettingsPanel.Children.Add(CreateRecolorSettings());
                }
                UpdateUndoRedoButtons();
            }
        }

        private void Undo() => MoveHistory(_undoStack, _redoStack);
        private void Redo() => MoveHistory(_redoStack, _undoStack);

        private void Undo_Click(object sender, RoutedEventArgs e) => Undo();
        private void Redo_Click(object sender, RoutedEventArgs e) => Redo();

        /// <summary>
        /// Обновляет доступность кнопок отмены и повтора.
        /// </summary>
        private void UpdateUndoRedoButtons()
        {
            UndoButton.IsEnabled = _undoStack.Count > 1;
            RedoButton.IsEnabled = _redoStack.Count > 0;
        }

        /// <summary>
        /// Сохраняет текущее отфильтрованное изображение в файл PNG.
        /// </summary>
        private void SaveResult_Click(object sender, RoutedEventArgs e)
        {
            if (_currentImage != null)
            {
                var dialog = new SaveFileDialog { Filter = "PNG Image|*.png", DefaultExt = "png" };
                if (dialog.ShowDialog() == true)
                    ImageLoader.SaveBitmapSource(_currentImage, dialog.FileName);
            }
        }
    }

    /// <summary>
    /// Описывает один фильтр в списке доступных фильтров окна <see cref="FilterWindow"/>.
    /// </summary>
    public class FilterItem
    {
        /// <summary>Отображаемое название фильтра.</summary>
        public string Name { get; set; }

        /// <summary>Фильтр изображения (если применим).</summary>
        public IImageFilter? ImageFilter { get; set; }

        /// <summary>Фильтр палитры (если применим).</summary>
        public IPaletteFilter? PaletteFilter { get; set; }

        /// <summary>Является ли фильтр цветовым (влияет на логику применения).</summary>
        public bool IsColorFilter { get; set; }

        /// <summary>Скрывать ли кнопку «Применить» (например, для ручной перекраски).</summary>
        public bool HideApplyButton { get; set; }
    }
}