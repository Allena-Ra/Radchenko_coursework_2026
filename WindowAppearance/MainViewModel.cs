using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32; // для OpenFileDialog
using ImageParser;

namespace WindowAppearance
{
    // <summary>
    /// ViewModel для главного окна приложения.
    /// Содержит логику загрузки изображения и отображения информации о нём.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private BitmapSource _loadedImage;
        private string _infoText;

        /// <summary>
        /// Загруженное изображение для отображения в UI.
        /// </summary>
        public BitmapSource LoadedImage
        {
            get => _loadedImage;
            set
            {
                _loadedImage = value;
                OnPropertyChanged(nameof(LoadedImage));
            }
        }

        /// <summary>
        /// Информационный текст о ширине, высоте и количестве пикселей.
        /// </summary>
        public string InfoText
        {
            get => _infoText;
            set
            {
                _infoText = value;
                OnPropertyChanged(nameof(InfoText));
            }
        }

        /// <summary>
        /// Команда для загрузки изображения.
        /// </summary>
        public ICommand LoadImageCommand { get; }

        public MainViewModel()
        {
            LoadImageCommand = new RelayCommand(LoadImage);
        }

        /// <summary>
        /// Метод, вызываемый при выполнении команды загрузки.
        /// Открывает диалог выбора файла, загружает изображение и отображает его.
        /// </summary>
        private void LoadImage()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Выберите изображение"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // 1. Загружаем данные с помощью ImageLoader
                    var (width, height, pixels) = ImageLoader.Load(dialog.FileName);

                    // 2. Создаём BitmapSource для отображения в WPF
                    var bitmap = new BitmapImage(new Uri(dialog.FileName));
                    LoadedImage = bitmap;

                    // 3. Формируем информационный текст
                    InfoText = $"Ширина: {width} px\nВысота: {height} px\nВсего пикселей: {width * height:N0}";
                }
                catch (Exception ex)
                {
                    InfoText = $"Ошибка загрузки: {ex.Message}";
                    LoadedImage = null;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
