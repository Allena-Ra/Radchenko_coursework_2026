using System;
using System.Drawing;
using System.Windows;
using Microsoft.Win32;
using Size = System.Drawing.Size;

namespace GmmImageSegmentator
{
    public partial class LoadSettingsWindow : Window
    {
        public string SelectedFilePath { get; private set; } = string.Empty;
        public double ScaleFactor { get; private set; } = 0.5;

        private Size originalSize;
        private bool fileSelected = false;

        public LoadSettingsWindow()
        {
            InitializeComponent();

            // После инициализации компонентов можно безопасно обращаться к элементам UI
            if (ScaleSlider != null)
                ScaleSlider.Value = 0.5;

            UpdateScaleDisplay();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp|Все файлы|*.*",
                Title = "Выберите изображение"
            };
            if (dialog.ShowDialog() == true)
            {
                SelectedFilePath = dialog.FileName;
                FilePathTextBox.Text = SelectedFilePath;
                fileSelected = true;

                using (var img = Image.FromFile(SelectedFilePath))
                {
                    originalSize = img.Size;
                }
                OriginalSizeText.Text = $"{originalSize.Width} × {originalSize.Height} px";
                UpdateScaledInfo();
            }
            else
            {
                fileSelected = false;
                OriginalSizeText.Text = "Файл не выбран";
                ScaledSizeText.Text = "—";
                ScaledPixelsText.Text = "—";
            }
        }

        private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ScaleFactor = ScaleSlider.Value;
            UpdateScaleDisplay();
            if (fileSelected)
                UpdateScaledInfo();
        }

        private void UpdateScaleDisplay()
        {
            // Проверка на null для безопасности (если метод вызван до инициализации компонента)
            if (ScaleValueText != null)
                ScaleValueText.Text = $"{ScaleFactor:P0}";
        }

        private void UpdateScaledInfo()
        {
            int newWidth = Math.Max(1, (int)(originalSize.Width * ScaleFactor));
            int newHeight = Math.Max(1, (int)(originalSize.Height * ScaleFactor));
            long totalPixels = newWidth * newHeight;
            if (ScaledSizeText != null)
                ScaledSizeText.Text = $"{newWidth} × {newHeight} px";
            if (ScaledPixelsText != null)
                ScaledPixelsText.Text = $"{totalPixels:N0} пикселей";
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!fileSelected)
            {
                MessageBox.Show("Пожалуйста, выберите файл изображения.", "Внимание",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}