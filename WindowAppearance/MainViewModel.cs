using GMMLogics.Implementations;
using GMMLogics.Interfaces;
using ImageParser;
using Microsoft.Win32; // для OpenFileDialog
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;
namespace WindowAppearance
{
    // <summary>
    /// ViewModel для главного окна приложения.
    /// Содержит логику загрузки изображения и отображения информации о нём.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        // === Поля ===
        private BitmapImage _originalImage;
        private ImageSource _segmentedImage;
        private string _imageInfo;
        private int _k = 3;                 // количество кластеров (по умолчанию 3)
        private double[][] _pixels;         // массив пикселей (нормализованные RGB)
        private int _imageWidth, _imageHeight;

        // === Свойства (привязка к UI) ===
        /// <summary>Исходное изображение</summary>
        public BitmapImage OriginalImage
        {
            get => _originalImage;
            set { _originalImage = value; OnPropertyChanged(); }
        }

        /// <summary>Сегментированное изображение (результат кластеризации)</summary>
        public ImageSource SegmentedImage
        {
            get => _segmentedImage;
            set { _segmentedImage = value; OnPropertyChanged(); }
        }

        /// <summary>Информация об изображении (размеры, количество пикселей, текущее число кластеров)</summary>
        public string ImageInfo
        {
            get => _imageInfo;
            set { _imageInfo = value; OnPropertyChanged(); }
        }

        /// <summary>Количество кластеров K</summary>
        public int K
        {
            get => _k;
            set
            {
                _k = value;
                OnPropertyChanged();
            }
        }

        // === Команды ===
        public ICommand LoadImageCommand { get; }
        public ICommand ClusterCommand { get; }

        /// <summary>Конструктор – инициализирует команды</summary>
        public MainViewModel()
        {
            LoadImageCommand = new RelayCommand(ExecuteLoadImage);
            ClusterCommand = new RelayCommand(ExecuteCluster, CanExecuteCluster);
        }

        // === Методы ===
        /// <summary>Выполняет загрузку изображения из файла и запускает кластеризацию</summary>
        private void ExecuteLoadImage(object parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp|Все файлы|*.*",
                Title = "Выберите изображение для сегментации"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Загружаем изображение через ImageLoader
                    (int width, int height, double[][] pixels) = ImageLoader.Load(openFileDialog.FileName);

                    _imageWidth = width;
                    _imageHeight = height;
                    _pixels = pixels;

                    // Отображаем оригинал
                    var bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                    OriginalImage = bitmap;

                    // Информация об изображении
                    int totalPixels = width * height;
                    ImageInfo = $"Ширина: {width} px\nВысота: {height} px\nПикселей: {totalPixels:N0}";

                    // Автоматически выполняем кластеризацию после загрузки
                    ExecuteCluster(null);
                }
                catch (Exception ex)
                {
                    ImageInfo = $"Ошибка загрузки: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
                }
            }
        }

        /// <summary>Проверяет, можно ли выполнить кластеризацию (данные загружены)</summary>
        private bool CanExecuteCluster(object parameter) => _pixels != null;

        /// <summary>Выполняет кластеризацию с текущим K и обновляет сегментированное изображение</summary>
        private void ExecuteCluster(object parameter)
        {
            if (_pixels == null)
            {
                Debug.WriteLine("ExecuteCluster: _pixels == null, кластеризация не выполняется");
                return;
            }

            Debug.WriteLine($"ExecuteCluster: начата кластеризация с K={K}");

            try
            {
                IClusteringAlgorithm algorithm = new AccordGMMAdapter();

                int[] labels = algorithm.Cluster(_pixels, K);
                Debug.WriteLine($"ExecuteCluster: получено меток {labels.Length}");

                var segmented = CreateSegmentedImage(labels);
                Debug.WriteLine($"ExecuteCluster: сегментированное изображение создано (null? {segmented == null})");

                SegmentedImage = segmented;
                Debug.WriteLine("ExecuteCluster: SegmentedImage присвоено");

                ImageInfo = $"Ширина: {_imageWidth} px\nВысота: {_imageHeight} px\nПикселей: {_pixels.Length:N0}\nКластеров: {K}";
                Debug.WriteLine("ExecuteCluster: завершено");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ExecuteCluster: исключение {ex.Message}\n{ex.StackTrace}");
                ImageInfo = $"Ошибка кластеризации: {ex.Message}";
            }
        }

        /// <summary>
        /// Создаёт изображение на основе меток кластеров.
        /// Каждый пиксель заменяется средним цветом его кластера.
        /// </summary>
        /// <param name="labels">Метки кластеров для всех пикселей</param>
        private ImageSource CreateSegmentedImage(int[] labels)
        {
            Debug.WriteLine($"CreateSegmentedImage: начало, labels.Length={labels.Length}, _imageWidth={_imageWidth}, _imageHeight={_imageHeight}, K={K}");
            int k = K;
            int channels = 3;

            // Вычисляем центры кластеров (средние RGB) для каждого кластера
            double[][] centers = new double[k][];
            int[] counts = new int[k];
            for (int i = 0; i < k; i++)
                centers[i] = new double[channels];

            for (int i = 0; i < labels.Length; i++)
            {
                int cluster = labels[i];
                counts[cluster]++;
                for (int c = 0; c < channels; c++)
                    centers[cluster][c] += _pixels[i][c];
            }
            for (int i = 0; i < k; i++)
            {
                if (counts[i] > 0)
                {
                    for (int c = 0; c < channels; c++)
                        centers[i][c] /= counts[i];
                }
            }

            // Формируем массив байтов RGB для всех пикселей
            byte[] pixelData = new byte[_imageWidth * _imageHeight * channels];
            int idx = 0;
            for (int i = 0; i < labels.Length; i++)
            {
                int cluster = labels[i];
                pixelData[idx++] = (byte)(centers[cluster][0] * 255);
                pixelData[idx++] = (byte)(centers[cluster][1] * 255);
                pixelData[idx++] = (byte)(centers[cluster][2] * 255);
            }

            // Создаём WriteableBitmap и заполняем его данными
            var wb = new WriteableBitmap(_imageWidth, _imageHeight, 96, 96, PixelFormats.Rgb24, null);
            wb.WritePixels(new Int32Rect(0, 0, _imageWidth, _imageHeight), pixelData, _imageWidth * channels, 0);
            return wb;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}