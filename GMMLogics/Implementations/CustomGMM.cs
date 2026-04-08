using System;
using GMMLogics.Interfaces;
using System.Threading.Tasks;

namespace GMMLogics.Implementations
{
    public class CustomGMM : IClusteringAlgorithm
    {
        public int MaxIterations { get; set; } = 100;
        public double Tolerance { get; set; } = 1e-3;

        public int[] Cluster(double[][] data, int k)
        {
            int n = data.Length;
            int dim = data[0].Length;
            Random rand = new Random(42);

            // Инициализация
            double[] pi = new double[k];
            double[][] means = new double[k][];
            double[][] vars = new double[k][]; // диагональные дисперсии (sigma^2)

            for (int j = 0; j < k; j++)
            {
                pi[j] = 1.0 / k;
                means[j] = new double[dim];
                vars[j] = new double[dim];
                int idx = rand.Next(n);
                Array.Copy(data[idx], means[j], dim);
                for (int d = 0; d < dim; d++)
                    vars[j][d] = 1e-2; // начальная дисперсия
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
                        double prob = pi[j] * DiagonalNormalPDF(data[i], means[j], vars[j]);
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

                // Переинициализация пустых кластеров
                for (int j = 0; j < k; j++)
                {
                    if (Nk[j] < 1.0)
                    {
                        int idx = rand.Next(n);
                        Array.Copy(data[idx], means[j], dim);
                        pi[j] = 1.0 / k;
                        for (int d = 0; d < dim; d++)
                            vars[j][d] = 1e-2;
                        Nk[j] = 1.0; // чтобы избежать деления на ноль
                    }
                }

                // Обновление весов
                for (int j = 0; j < k; j++)
                    pi[j] = Nk[j] / n;

                // Обновление средних
                for (int j = 0; j < k; j++)
                {
                    double[] newMean = new double[dim];
                    for (int i = 0; i < n; i++)
                        for (int d = 0; d < dim; d++)
                            newMean[d] += responsibilities[i][j] * data[i][d];
                    for (int d = 0; d < dim; d++)
                        newMean[d] /= Nk[j];
                    means[j] = newMean;
                }

                // Обновление дисперсий
                for (int j = 0; j < k; j++)
                {
                    double[] newVar = new double[dim];
                    for (int i = 0; i < n; i++)
                    {
                        double[] diff = new double[dim];
                        for (int d = 0; d < dim; d++)
                            diff[d] = data[i][d] - means[j][d];
                        for (int d = 0; d < dim; d++)
                            newVar[d] += responsibilities[i][j] * diff[d] * diff[d];
                    }
                    for (int d = 0; d < dim; d++)
                        newVar[d] /= Nk[j];
                    // Регуляризация
                    for (int d = 0; d < dim; d++)
                        newVar[d] += 1e-3;
                    vars[j] = newVar;
                }
            }

            // Формирование меток
            int[] labels = new int[n];
            for (int i = 0; i < n; i++)
            {
                double bestProb = -1;
                int bestCluster = 0;
                for (int j = 0; j < k; j++)
                {
                    double prob = pi[j] * DiagonalNormalPDF(data[i], means[j], vars[j]);
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
        /// Многомерное нормальное распределение с диагональной ковариацией.
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