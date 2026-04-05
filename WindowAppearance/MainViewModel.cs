using GmmImageSegmentator.Utilities;
using GMMLogics.Implementations;
using GMMLogics.Interfaces;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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

        public MainViewModel()
        {
            LoadImageCommand = new RelayCommand(ExecuteLoadImage);
            ClusterCommand = new RelayCommand(ExecuteCluster, CanExecuteCluster);
            SaveImageCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
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
                IClusteringAlgorithm algorithm = new AccordGMMAdapter();
                int[] labels = algorithm.Cluster(_pixels, K);
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