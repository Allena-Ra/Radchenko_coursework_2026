using GmmImageSegmentator.Utilities;
using GMMLogics.Implementations;
using GMMLogics.Interfaces;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WindowAppearance;

namespace GmmImageSegmentator
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private BitmapImage? _originalImage;
        private ImageSource? _segmentedImage;
        private string _imageInfo = string.Empty;
        private int _k = 3;
        private double[][]? _pixels;
        private int _imageWidth, _imageHeight;
        private int[] _labels;
        private List<System.Windows.Media.Color> _clusterMeanColors = new List<System.Windows.Media.Color>();

        public BitmapImage? OriginalImage
        {
            get => _originalImage;
            set { _originalImage = value; OnPropertyChanged(); }
        }

        public ImageSource? SegmentedImage
        {
            get => _segmentedImage;
            set { _segmentedImage = value; OnPropertyChanged(); }
        }

        public string ImageInfo
        {
            get => _imageInfo;
            set { _imageInfo = value; OnPropertyChanged(); }
        }

        public int K
        {
            get => _k;
            set { _k = value; OnPropertyChanged(); }
        }

        public ICommand LoadImageCommand { get; }
        public ICommand ClusterCommand { get; }
        public ICommand SaveImageCommand { get; }
        public ICommand OpenFiltersCommand { get; }
        public ICommand CompareWithAccordCommand { get; }
        public MainViewModel()
        {
            LoadImageCommand = new RelayCommand(ExecuteLoadImage);
            ClusterCommand = new RelayCommand(ExecuteCluster, CanExecuteCluster);
            SaveImageCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            OpenFiltersCommand = new RelayCommand(ExecuteOpenFilters);
            CompareWithAccordCommand = new RelayCommand(ExecuteCompareWithAccord);
        }

        private void ExecuteLoadImage(object parameter)
        {
            var settingsDialog = new LoadSettingsWindow();
            if (settingsDialog.ShowDialog() == true)
            {
                try
                {
                    var (width, height, pixels, displayImage) = ImageLoader.Load(settingsDialog.SelectedFilePath, settingsDialog.ScaleFactor);
                    _imageWidth = width;
                    _imageHeight = height;
                    _pixels = pixels;
                    OriginalImage = displayImage;

                    ImageInfo = $"Ширина: {width} px\nВысота: {height} px\nПикселей: {width * height:N0}\nМасштаб: {settingsDialog.ScaleFactor:P0}";
                }
                catch (Exception ex)
                {
                    ImageInfo = $"Ошибка загрузки: {ex.Message}";
                }
            }
        }

        private bool CanExecuteCluster(object parameter) => _pixels != null;

        private void ExecuteCluster(object parameter)
        {
            if (_pixels == null) return;

            try
            {
                IClusteringAlgorithm algorithm = new CustomGMMPredictor();
                int[] labels = algorithm.Cluster(_pixels, K);
                _labels = labels;

                // Вычисляем средние цвета кластеров
                _clusterMeanColors.Clear();
                for (int j = 0; j < K; j++)
                {
                    double r = 0, g = 0, b = 0;
                    int count = 0;
                    for (int i = 0; i < labels.Length; i++)
                    {
                        if (labels[i] == j)
                        {
                            r += _pixels[i][0];
                            g += _pixels[i][1];
                            b += _pixels[i][2];
                            count++;
                        }
                    }
                    if (count > 0)
                    {
                        r = r / count * 255;
                        g = g / count * 255;
                        b = b / count * 255;
                        _clusterMeanColors.Add(System.Windows.Media.Color.FromRgb((byte)r, (byte)g, (byte)b));
                    }
                    else
                        _clusterMeanColors.Add(System.Windows.Media.Colors.Black);
                }
                var segmented = ImageLoader.CreateSegmentedImage(labels, _pixels, _imageWidth, _imageHeight, K);
                SegmentedImage = segmented;
                ImageInfo = $"Ширина: {_imageWidth} px\nВысота: {_imageHeight} px\nПикселей: {_pixels.Length:N0}\nКластеров: {K}";
            }
            catch (Exception ex)
            {
                ImageInfo = $"Ошибка кластеризации: {ex.Message}";
                Debug.WriteLine(ex.ToString());
            }
        }
        private void ExecuteOpenFilters(object parameter)
        {
            if (_labels == null) return;
            var filterWindow = new FilterWindow(_labels, _imageWidth, _imageHeight, K, _clusterMeanColors);
            filterWindow.Owner = Application.Current.MainWindow;
            filterWindow.ShowDialog();
        }

        private void ExecuteCompareWithAccord(object parameter)
        {
            if (_pixels == null) return;
            var accord = new AccordGMMAdapter();
            int[] accordLabels = accord.Cluster(_pixels, K);
            var accordImage = ImageLoader.CreateSegmentedImage(accordLabels, _pixels, _imageWidth, _imageHeight, K);
            var customImage = SegmentedImage as BitmapImage; // текущее изображение от CustomGMM
            var compareWindow = new ComparisonWindow(customImage, accordImage);
            compareWindow.Owner = Application.Current.MainWindow;
            compareWindow.ShowDialog();
        }

        private bool CanExecuteSave(object parameter) => SegmentedImage != null;

        private void ExecuteSave(object parameter)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|Все файлы|*.*",
                Title = "Сохранить сегментированное изображение",
                DefaultExt = "png"
            };
            if (saveDialog.ShowDialog() == true && SegmentedImage is BitmapSource bs)
            {
                try
                {
                    ImageLoader.SaveBitmapSource(bs, saveDialog.FileName);
                    ImageInfo = $"Сохранено: {saveDialog.FileName}";
                }
                catch (Exception ex)
                {
                    ImageInfo = $"Ошибка сохранения: {ex.Message}";
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}