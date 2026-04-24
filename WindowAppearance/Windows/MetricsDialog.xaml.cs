using System.Windows;

namespace GmmImageSegmentator
{
    /// <summary>
    /// Диалоговое окно, показывающее метрики качества кластеризации
    /// для двух алгоритмов и памятку по интерпретации.
    /// </summary>
    public partial class MetricsDialog : Window
    {
        /// <summary>
        /// Создаёт диалог с переданными метриками.
        /// </summary>
        /// <param name="customMetrics">Метрики Custom GMM (многострочная строка).</param>
        /// <param name="accordMetrics">Метрики Accord.NET GMM.</param>
        public MetricsDialog(string customMetrics, string accordMetrics)
        {
            InitializeComponent();
            CustomMetricsText.Text = customMetrics;
            AccordMetricsText.Text = accordMetrics;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}