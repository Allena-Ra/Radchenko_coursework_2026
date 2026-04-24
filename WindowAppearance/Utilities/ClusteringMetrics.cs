namespace GmmImageSegmentator.Utilities
{
    public static class ClusteringMetrics
    {
        /// <summary>
        /// Вычисляет коэффициент силуэта для подвыборки (для производительности).
        /// Возвращает среднее значение по всем точкам выборки.
        /// </summary>
        /// <param name="data">Полные данные (или подвыборка).</param>
        /// <param name="labels">Метки кластеров.</param>
        /// <param name="sampleSize">Размер подвыборки для расчёта (по умолчанию 1000).</param>
        /// <returns>Средний силуэт в диапазоне [-1, 1].</returns>
        public static double SilhouetteScore(double[][] data, int[] labels, int sampleSize = 1000)
        {
            int n = data.Length;
            if (n <= 1) return 0;

            // Определяем число кластеров
            int k = labels.Max() + 1;

            // Предварительно вычисляем центры кластеров
            var centers = new double[k][];
            var counts = new int[k];
            for (int j = 0; j < k; j++) centers[j] = new double[data[0].Length];

            for (int i = 0; i < n; i++)
            {
                int c = labels[i];
                counts[c]++;
                for (int d = 0; d < data[i].Length; d++)
                    centers[c][d] += data[i][d];
            }
            for (int j = 0; j < k; j++)
                if (counts[j] > 0)
                    for (int d = 0; d < centers[j].Length; d++)
                        centers[j][d] /= counts[j];

            // Случайная подвыборка
            var rnd = new Random(42);
            var indices = Enumerable.Range(0, n).OrderBy(_ => rnd.Next()).Take(sampleSize).ToArray();

            double totalSilhouette = 0;
            foreach (int i in indices)
            {
                int label = labels[i];
                double a = 0; // среднее расстояние до точек своего кластера
                double b = double.MaxValue; // минимальное среднее расстояние до другого кластера
                int ownCount = 0;

                // Расстояния до центров для скорости (приближение)
                a = EuclideanDistance(data[i], centers[label]);

                // Ищем ближайший другой кластер
                for (int j = 0; j < k; j++)
                {
                    if (j == label) continue;
                    double dist = EuclideanDistance(data[i], centers[j]);
                    if (dist < b) b = dist;
                }

                double s = (b - a) / Math.Max(a, b);
                totalSilhouette += s;
            }

            return totalSilhouette / indices.Length;
        }

        /// <summary>
        /// Вычисляет BIC (Bayesian Information Criterion) для GMM.
        /// </summary>
        /// <param name="logLikelihood">Логарифм правдоподобия модели.</param>
        /// <param name="numDataPoints">Количество точек данных.</param>
        /// <param name="numComponents">Количество компонент (кластеров).</param>
        /// <param name="dimension">Размерность данных (для RGB = 3).</param>
        /// <returns>Значение BIC (меньше – лучше).</returns>
        public static double BIC(double logLikelihood, int numDataPoints, int numComponents, int dimension = 3)
        {
            // Число параметров: веса (k-1) + средние (k*dim) + дисперсии (k*dim)
            int k = numComponents;
            int dim = dimension;
            int numParams = (k - 1) + 2 * k * dim;
            double bic = numParams * Math.Log(numDataPoints) - 2 * logLikelihood;
            return bic;
        }

        /// <summary>
        /// Вычисляет AIC (Akaike Information Criterion).
        /// </summary>
        public static double AIC(double logLikelihood, int numComponents, int dimension = 3)
        {
            int k = numComponents;
            int dim = dimension;
            int numParams = (k - 1) + 2 * k * dim;
            return 2 * numParams - 2 * logLikelihood;
        }

        private static double EuclideanDistance(double[] a, double[] b)
        {
            double sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                double diff = a[i] - b[i];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
        }

        /// <summary>
        /// Формирует строку с основными метриками качества кластеризации.
        /// </summary>
        /// <param name="logLikelihood">Логарифм правдоподобия модели.</param>
        /// <param name="data">Исходные данные (пиксели).</param>
        /// <param name="labels">Метки кластеров.</param>
        /// <param name="k">Количество кластеров.</param>
        /// <returns>Многострочная строка с метриками.</returns>
        public static string FormatMetrics(double logLikelihood, double[][] data, int[] labels, int k)
        {
            int n = data.Length;
            double bic = BIC(logLikelihood, n, k);
            double aic = AIC(logLikelihood, k);
            double silhouette = SilhouetteScore(data, labels, sampleSize: Math.Min(1000, n));
            return $"Log-Likelihood: {logLikelihood:F2}\n" +
                   $"BIC: {bic:F2}\n" +
                   $"AIC: {aic:F2}\n" +
                   $"Silhouette: {silhouette:F3}";
        }
    }
}