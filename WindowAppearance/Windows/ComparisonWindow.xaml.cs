using System.Windows;
using System.Windows.Media.Imaging;
using static GmmImageSegmentator.MainViewModel;

namespace GmmImageSegmentator
{
    /// <summary>
    /// Окно сравнения результатов двух алгоритмов с возможностью просмотра метрик.
    /// </summary>
    public partial class ComparisonWindow : Window
    {
        /// <summary>
        /// Результат сравнения, выбранный пользователем.
        /// </summary>
        public ComparisonResult SelectedResult { get; private set; }

        private readonly string _customMetrics;
        private readonly string _accordMetrics;

        /// <summary>
        /// Создаёт окно сравнения.
        /// </summary>
        /// <param name="customImage">Изображение, сегментированное моим алгоритмом </param>
        /// <param name="accordImage">Изображение, сегментированное Accord.NET.</param>
        /// <param name="customMetrics">Метрики моего алгоритма </param>
        /// <param name="accordMetrics">Метрики Accord.NET GMM.</param>
        public ComparisonWindow(BitmapImage customImage, BitmapImage accordImage,
                                string customMetrics, string accordMetrics)
        {
            InitializeComponent();
            CustomImageControl.Source = customImage;
            AccordImageControl.Source = accordImage;
            _customMetrics = customMetrics;
            _accordMetrics = accordMetrics;
        }

        private void UseCustom_Click(object sender, RoutedEventArgs e)
        {
            SelectedResult = ComparisonResult.Custom;
            DialogResult = true;
            Close();
        }

        private void UseAccord_Click(object sender, RoutedEventArgs e)
        {
            SelectedResult = ComparisonResult.Accord;
            DialogResult = true;
            Close();
        }

        private void ShowMetrics_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MetricsDialog(_customMetrics, _accordMetrics);
            dialog.Owner = this;
            dialog.ShowDialog();
        }
    }
}