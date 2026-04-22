 using System;
using GMMLogics.Interfaces;

namespace GMMLogics.Implementations
{
    /// <summary>
    /// Собственная реализация GMM с возможностью обучения на подвыборке.
    /// </summary>
    public class CustomGMMPredictor : IClusteringAlgorithm
    {
        public int MaxIterations { get; set; } = 100;
        public double Tolerance { get; set; } = 1e-3;
        public double SampleRatio { get; set; } = 0.2; // доля пикселей для обучения (20%)

        // Параметры модели (сохраняются после обучения)
        private double[] _pi;
        private double[][] _means;
        private double[][] _vars;

        /// <summary>
        /// Обучение модели на подвыборке данных.
        /// </summary>
        /// <param name="data">Все данные (обычно все пиксели).</param>
        /// <param name="k">Количество кластеров.</param>
        public void Train(double[][] data, int k)
        {
            int n = data.Length;
            int dim = data[0].Length;

            // Создаём подвыборку
            int sampleSize = Math.Max(1, (int)(n * SampleRatio));
            double[][] sample = new double[sampleSize][];
            Random rand = new Random(42);
            for (int i = 0; i < sampleSize; i++)
            {
                int idx = rand.Next(n);
                sample[i] = data[idx];
            }

            // --- Инициализация параметров ---
            _pi = new double[k];
            _means = new double[k][];
            _vars = new double[k][];

            for (int j = 0; j < k; j++)
            {
                _pi[j] = 1.0 / k;
                _means[j] = new double[dim];
                int idx = rand.Next(sampleSize);
                Array.Copy(sample[idx], _means[j], dim);

                _vars[j] = new double[dim];
                for (int d = 0; d < dim; d++)
                    _vars[j][d] = 1e-2;
            }

            // --- EM-цикл на подвыборке ---
            double prevLogLikelihood = double.NegativeInfinity;
            for (int iter = 0; iter < MaxIterations; iter++)
            {
                // E-шаг
                double[][] responsibilities = new double[sampleSize][];
                double logLikelihood = 0.0;

                for (int i = 0; i < sampleSize; i++)
                {
                    responsibilities[i] = new double[k];
                    double sum = 0.0;
                    for (int j = 0; j < k; j++)
                    {
                        double prob = _pi[j] * DiagonalNormalPDF(sample[i], _means[j], _vars[j]);
                        responsibilities[i][j] = prob;
                        sum += prob;
                    }
                    for (int j = 0; j < k; j++)
                        responsibilities[i][j] /= sum;
                    logLikelihood += Math.Log(sum);
                }

                if (Math.Abs(logLikelihood - prevLogLikelihood) < Tolerance)
                    break;
                prevLogLikelihood = logLikelihood;

                // M-шаг
                double[] Nk = new double[k];
                for (int i = 0; i < sampleSize; i++)
                    for (int j = 0; j < k; j++)
                        Nk[j] += responsibilities[i][j];

                // Переинициализация пустых кластеров
                for (int j = 0; j < k; j++)
                {
                    if (Nk[j] < 1.0)
                    {
                        int idx = rand.Next(sampleSize);
                        Array.Copy(sample[idx], _means[j], dim);
                        _pi[j] = 1.0 / k;
                        for (int d = 0; d < dim; d++)
                            _vars[j][d] = 1e-2;
                        Nk[j] = 1.0;
                    }
                }

                // Обновление весов
                for (int j = 0; j < k; j++)
                    _pi[j] = Nk[j] / sampleSize;

                // Обновление средних
                for (int j = 0; j < k; j++)
                {
                    double[] newMean = new double[dim];
                    for (int i = 0; i < sampleSize; i++)
                        for (int d = 0; d < dim; d++)
                            newMean[d] += responsibilities[i][j] * sample[i][d];
                    for (int d = 0; d < dim; d++)
                        newMean[d] /= Nk[j];
                    _means[j] = newMean;
                }

                // Обновление дисперсий
                for (int j = 0; j < k; j++)
                {
                    double[] newVar = new double[dim];
                    for (int i = 0; i < sampleSize; i++)
                    {
                        double[] diff = new double[dim];
                        for (int d = 0; d < dim; d++)
                            diff[d] = sample[i][d] - _means[j][d];
                        for (int d = 0; d < dim; d++)
                            newVar[d] += responsibilities[i][j] * diff[d] * diff[d];
                    }
                    for (int d = 0; d < dim; d++)
                        newVar[d] /= Nk[j];
                    // Регуляризация
                    for (int d = 0; d < dim; d++)
                        newVar[d] += 1e-3;
                    _vars[j] = newVar;
                }
            }
        }

        /// <summary>
        /// Предсказание меток для всех данных на основе обученной модели.
        /// </summary>
        public int[] Predict(double[][] data)
        {
            if (_pi == null) throw new InvalidOperationException("Модель не обучена. Вызовите Train() сначала.");
            int n = data.Length;
            int k = _pi.Length;
            int[] labels = new int[n];
            for (int i = 0; i < n; i++)
            {
                double bestProb = -1;
                int bestCluster = 0;
                for (int j = 0; j < k; j++)
                {
                    double prob = _pi[j] * DiagonalNormalPDF(data[i], _means[j], _vars[j]);
                    if (prob > bestProb)
                    {
                        bestProb = prob;
                        bestCluster = j;
                    }
                }
                labels[i] = bestCluster;
            }
            return labels;
        }

        /// <summary>
        /// Полная кластеризация: обучение на всех данных (без подвыборки) – для обратной совместимости.
        /// </summary>
        public int[] Cluster(double[][] data, int k)
        {
            // Если SampleRatio < 1, обучаем на подвыборке, затем предсказываем для всех.
            if (SampleRatio < 0.999)
            {
                Train(data, k);
                return Predict(data);
            }
            else
            {
                // Полное обучение (без подвыборки)
                TrainFull(data, k);
                return Predict(data);
            }
        }

        /// <summary>
        /// Обучение на всех данных (используется при SampleRatio = 1).
        /// </summary>
        private void TrainFull(double[][] data, int k)
        {
            int n = data.Length;
            int dim = data[0].Length;
            Random rand = new Random(42);

            _pi = new double[k];
            _means = new double[k][];
            _vars = new double[k][];

            for (int j = 0; j < k; j++)
            {
                _pi[j] = 1.0 / k;
                _means[j] = new double[dim];
                int idx = rand.Next(n);
                Array.Copy(data[idx], _means[j], dim);
                _vars[j] = new double[dim];
                for (int d = 0; d < dim; d++)
                    _vars[j][d] = 1e-2;
            }

            double prevLogLikelihood = double.NegativeInfinity;
            for (int iter = 0; iter < MaxIterations; iter++)
            {
                // E-шаг (аналогично Train, но на всех данных)
                double[][] responsibilities = new double[n][];
                double logLikelihood = 0.0;
                for (int i = 0; i < n; i++)
                {
                    responsibilities[i] = new double[k];
                    double sum = 0.0;
                    for (int j = 0; j < k; j++)
                    {
                        double prob = _pi[j] * DiagonalNormalPDF(data[i], _means[j], _vars[j]);
                        responsibilities[i][j] = prob;
                        sum += prob;
                    }
                    for (int j = 0; j < k; j++)
                        responsibilities[i][j] /= sum;
                    logLikelihood += Math.Log(sum);
                }
                if (Math.Abs(logLikelihood - prevLogLikelihood) < Tolerance)
                    break;
                prevLogLikelihood = logLikelihood;

                double[] Nk = new double[k];
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < k; j++)
                        Nk[j] += responsibilities[i][j];

                for (int j = 0; j < k; j++)
                {
                    if (Nk[j] < 1.0)
                    {
                        int idx = rand.Next(n);
                        Array.Copy(data[idx], _means[j], dim);
                        _pi[j] = 1.0 / k;
                        for (int d = 0; d < dim; d++)
                            _vars[j][d] = 1e-2;
                        Nk[j] = 1.0;
                    }
                }

                for (int j = 0; j < k; j++)
                    _pi[j] = Nk[j] / n;

                for (int j = 0; j < k; j++)
                {
                    double[] newMean = new double[dim];
                    for (int i = 0; i < n; i++)
                        for (int d = 0; d < dim; d++)
                            newMean[d] += responsibilities[i][j] * data[i][d];
                    for (int d = 0; d < dim; d++)
                        newMean[d] /= Nk[j];
                    _means[j] = newMean;
                }

                for (int j = 0; j < k; j++)
                {
                    double[] newVar = new double[dim];
                    for (int i = 0; i < n; i++)
                    {
                        double[] diff = new double[dim];
                        for (int d = 0; d < dim; d++)
                            diff[d] = data[i][d] - _means[j][d];
                        for (int d = 0; d < dim; d++)
                            newVar[d] += responsibilities[i][j] * diff[d] * diff[d];
                    }
                    for (int d = 0; d < dim; d++)
                        newVar[d] /= Nk[j];
                    for (int d = 0; d < dim; d++)
                        newVar[d] += 1e-3;
                    _vars[j] = newVar;
                }
            }
        }

        /// <summary>
        /// Вычисляет значение диагонального многомерного нормального распределения.
        /// </summary>
        private double DiagonalNormalPDF(double[] x, double[] mean, double[] var)
        {
            int dim = x.Length;
            double exponent = 0.0;
            double det = 1.0;
            for (int d = 0; d < dim; d++)
            {
                double diff = x[d] - mean[d];
                exponent += diff * diff / var[d];
                det *= var[d];
            }
            exponent *= -0.5;
            double normConst = 1.0 / (Math.Pow(2 * Math.PI, dim / 2.0) * Math.Sqrt(det));
            return normConst * Math.Exp(exponent);
        }
    }
}