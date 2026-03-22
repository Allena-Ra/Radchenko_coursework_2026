using Accord.MachineLearning;
using GMMLogics.Interfaces;
using System;

namespace GMMLogics.Implementations
{
    /// <summary>
    /// Адаптер для реализации GMM из библиотеки Accord.NET.
    /// </summary>
    public class AccordGMMAdapter : IClusteringAlgorithm
    {
        /// <summary>
        /// Выполняет кластеризацию с помощью Accord.NET GaussianMixtureModel.
        /// </summary>
        /// <param name="data">Входные данные (точки)</param>
        /// <param name="k">количество кластеров</param>
        /// <returns>Массивтметок кластеров.</returns>
        public int[] Cluster(double[][] data, int k)
        {
            // небольшой шум для предотвращения вырожденных ковариационных матриц (найдено на просторах маил.ру)
            var noisyData = new double[data.Length][];
            var rand = new Random(42);
            for (int i = 0; i < data.Length; i++)
            {
                noisyData[i] = new double[data[i].Length];
                for (int j = 0; j < data[i].Length; j++)
                {
                    noisyData[i][j] = data[i][j] + (rand.NextDouble() - 0.5) * 1e-6;
                }
            }

            var gmm = new GaussianMixtureModel(k)
            {
                MaxIterations = 100,
                Tolerance = 1e-3
            };

            var clusters = gmm.Learn(noisyData);
            return clusters.Decide(noisyData);
        }
    }
}