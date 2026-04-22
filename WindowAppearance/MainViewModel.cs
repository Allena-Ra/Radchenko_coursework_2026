using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using GmmImageSegmentator.Utilities;
using GMMLogics.Implementations;
using GMMLogics.Interfaces;

namespace GmmImageSegmentator
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // === Поля ===
        private BitmapImage? _originalImage;
        private ImageSource? _segmentedImage;
        private string _imageInfo = string.Empty;
        private int _k = 3;
        private double[][]? _pixels;
        private int _imageWidth, _imageHeight;
        private int[] _labels = Array.Empty<int>();
        private List<Color> _clusterMeanColors = new();
        private bool _isClustering = false;
        private string _processingMessage = "";
        private bool _isSegmented = false;
        private List<Color> _currentPalette = new();
        private BitmapImage? _customResultImage;
        private BitmapImage? _accordResultImage;
        private ObservableCollection<Color> _userPalette = new();

        // === Перечисления ===
        public enum ResultSource { Custom, Accord }
        public enum ComparisonResult { Custom, Accord }

        private ResultSource _activeResult = ResultSource.Custom;

        // === Свойства для привязки ===
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

        public bool IsClustering
        {
            get => _isClustering;
            set { _isClustering = value; OnPropertyChanged(); }
        }

        public string ProcessingMessage
        {
            get => _processingMessage;
            set { _processingMessage = value; OnPropertyChanged(); }
        }

        public bool IsSegmented
        {
            get => _isSegmented;
            set
            {
                _isSegmented = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilterButtonsVisibility));
                OnPropertyChanged(nameof(ComparisonButtonsVisibility));
            }
        }

        public Visibility FilterButtonsVisibility => IsSegmented ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ComparisonButtonsVisibility => IsSegmented ? Visibility.Visible : Visibility.Collapsed;

        public ObservableCollection<Color> UserPalette
        {
            get => _userPalette;
            set { _userPalette = value; OnPropertyChanged(); }
        }

        public List<Color> CurrentPalette
        {
            get => _currentPalette;
            set { _currentPalette = value; OnPropertyChanged(); }
        }

        public ResultSource ActiveResult
        {
            get => _activeResult;
            set { _activeResult = value; OnPropertyChanged(); SaveCurrentResultAsActive(); }
        }

        // === Команды ===
        public ICommand LoadImageCommand { get; }
        public ICommand ClusterCommand { get; }
        public ICommand SaveImageCommand { get; }
        public ICommand OpenFiltersCommand { get; }
        public ICommand CompareWithAccordCommand { get; }

        // === Конструктор ===
        public MainViewModel()
        {
            LoadImageCommand = new RelayCommand(ExecuteLoadImage);
            ClusterCommand = new RelayCommand(ExecuteCluster, CanExecuteCluster);
            SaveImageCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            OpenFiltersCommand = new RelayCommand(ExecuteOpenFilters);
            CompareWithAccordCommand = new RelayCommand(ExecuteCompareWithAccord);
        }

        // === Методы ===

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
                    ImageInfo = $"\tШирина: {width} px Высота: {height} px Пикселей: {width * height:N0} Масштаб: {settingsDialog.ScaleFactor:P0}";
                }
                catch (Exception ex)
                {
                    ImageInfo = $"Ошибка загрузки: {ex.Message}";
                }
            }
        }

        private bool CanExecuteCluster(object parameter) => _pixels != null && !IsClustering;

        private async void ExecuteCluster(object parameter)
        {
            if (_pixels == null || IsClustering) return;

            IsClustering = true;
            IsSegmented = false;
            _animationCts?.Cancel();
            _animationCts = new CancellationTokenSource();

            try
            {
                var result = await Task.Run(() =>
                {
                    _ = AnimateMessageAsync("Изучаю структуру цветов", _animationCts.Token);
                    var algorithm = new CustomGMMPredictor();
                    return algorithm.Cluster(_pixels, K);
                }, _animationCts.Token);

                _animationCts.Cancel();
                ProcessingMessage = "Рисую результат...";
                UpdateFromLabels(result);
                RefreshSegmentedImage();
                IsSegmented = true;
                ImageInfo = $"\tШирина: {_imageWidth} px Высота: {_imageHeight} px Пикселей: {_pixels.Length:N0} Кластеров: {K}";
            }
            catch (OperationCanceledException)
            {
                ProcessingMessage = "Отменено";
            }
            catch (Exception ex)
            {
                ImageInfo = $"Ошибка кластеризации: {ex.Message}";
                ProcessingMessage = "Ошибка при обработке";
            }
            finally
            {
                _animationCts?.Cancel();
                IsClustering = false;
                ProcessingMessage = "";
            }
        }

        /// <summary>
        /// Обновляет внутренние данные (метки, средние цвета, палитры) на основе новых меток.
        /// </summary>
        private void UpdateFromLabels(int[] labels)
        {
            _labels = labels;
            int actualK = labels.Length > 0 ? labels.Max() + 1 : K;
            if (actualK != K)
            {
                K = actualK;
                OnPropertyChanged(nameof(K));
            }

            _clusterMeanColors.Clear();
            for (int j = 0; j < actualK; j++)
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
                    _clusterMeanColors.Add(Color.FromRgb((byte)r, (byte)g, (byte)b));
                }
                else
                {
                    _clusterMeanColors.Add(Colors.Black);
                }
            }

            _currentPalette = new List<Color>(_clusterMeanColors);
            UserPalette.Clear();
            foreach (var c in _clusterMeanColors)
                UserPalette.Add(c);
        }

        private void RefreshSegmentedImage()
        {
            if (_labels == null || _labels.Length == 0) return;
            if (_currentPalette == null || _currentPalette.Count == 0)
                _currentPalette = new List<Color>(_clusterMeanColors);

            var image = ImageLoader.CreateSegmentedImageFromColors(_labels, _imageWidth, _imageHeight, _currentPalette);
            SegmentedImage = image;
        }

        private void ExecuteOpenFilters(object parameter)
        {
            if (_labels == null || _labels.Length == 0) return;
            var filterWindow = new FilterWindow(_labels, _imageWidth, _imageHeight, K, _clusterMeanColors);
            filterWindow.Owner = Application.Current.MainWindow;
            filterWindow.ShowDialog();
        }

        private async void ExecuteCompareWithAccord(object parameter)
        {
            if (_pixels == null) return;

            _animationCts?.Cancel();
            _animationCts = new CancellationTokenSource();
            _ = AnimateMessageAsync("Запуск Accord", _animationCts.Token);
            IsClustering = true;

            try
            {
                var accordLabels = await Task.Run(() => new AccordGMMAdapter().Cluster(_pixels, K), _animationCts.Token);
                var accordImage = ImageLoader.CreateSegmentedImage(accordLabels, _pixels, _imageWidth, _imageHeight, K);
                _accordResultImage = accordImage;

                _animationCts.Cancel();
                ProcessingMessage = "";

                var compareWindow = new ComparisonWindow(SegmentedImage as BitmapImage, accordImage);
                compareWindow.Owner = Application.Current.MainWindow;
                if (compareWindow.ShowDialog() == true && compareWindow.SelectedResult == ComparisonResult.Accord)
                {
                    UpdateFromLabels(accordLabels);
                    SegmentedImage = accordImage;
                }
            }
            catch (OperationCanceledException)
            {
                ProcessingMessage = "Отменено";
            }
            catch (Exception ex)
            {
                ImageInfo = $"Ошибка сравнения: {ex.Message}";
            }
            finally
            {
                _animationCts?.Cancel();
                IsClustering = false;
                ProcessingMessage = "";
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

        private void SaveCurrentResultAsActive()
        {
            if (ActiveResult == ResultSource.Custom && SegmentedImage != null)
                _customResultImage = SegmentedImage as BitmapImage;
            else if (ActiveResult == ResultSource.Accord && _accordResultImage != null)
                SegmentedImage = _accordResultImage;
        }

        // Это для красоты...
        private CancellationTokenSource? _animationCts;
        private int _dotCount = 0;

        private async Task AnimateMessageAsync(string baseMessage, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                _dotCount = (_dotCount % 3) + 1;
                ProcessingMessage = baseMessage + new string('.', _dotCount);
                await Task.Delay(500, token);
            }
        }

        // === INotifyPropertyChanged ===
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}