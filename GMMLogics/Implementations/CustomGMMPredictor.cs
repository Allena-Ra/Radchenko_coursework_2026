using System;
using GMMLogics.Interfaces;

namespace GMMLogics.Implementations
{
    /// <summary>
    /// Собственная реализация кластеризации на основе Гауссовых смесей (GMM)
    /// с EM-алгоритмом и опциональным обучением на подвыборке для ускорения.
    /// </summary>
    public class CustomGMMPredictor : IClusteringAlgorithm
    {
        /// <summary>Максимальное число итераций EM.</summary>
        public int MaxIterations { get; set; } = 100;

        /// <summary>Порог сходимости по логарифмическому правдоподобию.</summary>
        public double Tolerance { get; set; } = 1e-3;

        /// <summary>Доля данных, используемая при обучении (0 < SampleRatio ≤ 1).</summary>
        public double SampleRatio { get; set; } = 0.2;
        /// <summary>Логарифм правдоподобия, достигнутый на последней итерации обучения.</summary>
        public double LastLogLikelihood { get; private set; }
        // Параметры смеси
        private double[] _pi;       // веса компонент
        private double[][] _means;  // средние (по каждой размерности)
        private double[][] _vars;   // дисперсии (диагональная ковариация)

        /// <summary>
        /// Обучает модель GMM на подвыборке из данных.
        /// </summary>
        /// <param name="data">Полный набор данных (обычно все пиксели).</param>
        /// <param name="k">Количество компонент смеси.</param>
        public void Train(double[][] data, int k)
        {
            int n = data.Length;
            int sampleSize = Math.Max(1, (int)(n * SampleRatio));
            double[][] sample = new double[sampleSize][];
            Random rand = new Random(42);
            for (int i = 0; i < sampleSize; i++)
            {
                sample[i] = data[rand.Next(n)];
            }

            RunEM(sample, k, rand);
        }

        /// <summary>
        /// Предсказывает метки кластеров для всех переданных точек на основе обученной модели.
        /// </summary>
        /// <param name="data">Данные для классификации.</param>
        /// <returns>Массив меток кластеров.</returns>
        /// <exception cref="InvalidOperationException">Модель не обучена.</exception>
        public int[] Predict(double[][] data)
        {
            if (_pi == null)
                throw new InvalidOperationException("Модель не обучена. Вызовите Train() сначала.");

            int n = data.Length;
            int k = _pi.Length;
            int[] labels = new int[n];

            for (int i = 0; i < n; i++)
            {
                double bestProb = double.MinValue;
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
        /// Выполняет полную кластеризацию: обучает модель и возвращает метки.
        /// Если <see cref="SampleRatio"/> меньше 1, обучение идёт на подвыборке,
        /// затем предсказание – для всех данных.
        /// </summary>
        public int[] Cluster(double[][] data, int k)
        {
            if (SampleRatio < 0.999)
            {
                Train(data, k);
                return Predict(data);
            }
            else
            {
                TrainFull(data, k);
                return Predict(data);
            }
        }

        /// <summary>
        /// Обучает модель на полном наборе данных (без подвыборки).
        /// Используется, когда <see cref="SampleRatio"/> ≈ 1.
        /// </summary>
        private void TrainFull(double[][] data, int k)
        {
            RunEM(data, k, new Random(42));
        }

        /// <summary>
        /// Общий метод EM-алгоритма. Инициализирует параметры и выполняет итерации
        /// до сходимости или исчерпания лимита итераций.
        /// </summary>
        /// <param name="trainingData">Данные для обучения (подвыборка или полный набор).</param>
        /// <param name="k">Число компонент.</param>
        /// <param name="rand">Генератор случайных чисел (с фиксированным seed для воспроизводимости).</param>
        private void RunEM(double[][] trainingData, int k, Random rand)
        {
            int n = trainingData.Length;
            int dim = trainingData[0].Length;

            // Инициализация параметров
            _pi = new double[k];
            _means = new double[k][];
            _vars = new double[k][];

            for (int j = 0; j < k; j++)
            {
                _pi[j] = 1.0 / k;
                _means[j] = new double[dim];
                int idx = rand.Next(n);
                Array.Copy(trainingData[idx], _means[j], dim);
                _vars[j] = new double[dim];
                for (int d = 0; d < dim; d++)
                    _vars[j][d] = 1e-2;
            }

            double prevLogLikelihood = double.NegativeInfinity;
            for (int iter = 0; iter < MaxIterations; iter++)
            {
                // E-шаг
                double[][] responsibilities = new double[n][];
                double logLikelihood = 0.0;

                for (int i = 0; i < n; i++)
                {
                    responsibilities[i] = new double[k];
                    double sum = 0.0;
                    for (int j = 0; j < k; j++)
                    {
                        double prob = _pi[j] * DiagonalNormalPDF(trainingData[i], _means[j], _vars[j]);
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
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < k; j++)
                        Nk[j] += responsibilities[i][j];

                // Переинициализация вырожденных кластеров
                for (int j = 0; j < k; j++)
                {
                    if (Nk[j] < 1.0)
                    {
                        int idx = rand.Next(n);
                        Array.Copy(trainingData[idx], _means[j], dim);
                        _pi[j] = 1.0 / k;
                        for (int d = 0; d < dim; d++)
                            _vars[j][d] = 1e-2;
                        Nk[j] = 1.0; // предотвращаем деление на ноль
                    }
                }

                // Обновление весов
                for (int j = 0; j < k; j++)
                    _pi[j] = Nk[j] / n;

                // Обновление средних
                for (int j = 0; j < k; j++)
                {
                    double[] newMean = new double[dim];
                    for (int i = 0; i < n; i++)
                        for (int d = 0; d < dim; d++)
                            newMean[d] += responsibilities[i][j] * trainingData[i][d];
                    for (int d = 0; d < dim; d++)
                        newMean[d] /= Nk[j];
                    _means[j] = newMean;
                }

                // Обновление дисперсий
                for (int j = 0; j < k; j++)
                {
                    double[] newVar = new double[dim];
                    for (int i = 0; i < n; i++)
                    {
                        double[] diff = new double[dim];
                        for (int d = 0; d < dim; d++)
                            diff[d] = trainingData[i][d] - _means[j][d];
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
                LastLogLikelihood = logLikelihood;
            }
        }

        /// <summary>
        /// Вычисляет значение многомерного нормального распределения с диагональной
        /// ковариационной матрицей (произведение одномерных нормальных распределений).
        /// </summary>
        private static double DiagonalNormalPDF(double[] x, double[] mean, double[] var)
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