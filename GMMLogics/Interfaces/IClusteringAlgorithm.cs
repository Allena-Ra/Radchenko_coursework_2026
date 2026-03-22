namespace GMMLogics.Interfaces
{
    /// <summary>
    /// Интерфейс для алгоритмов кластеризации (моего и аккорд)
    /// </summary>
    public interface IClusteringAlgorithm
    {
        /// <summary>
        /// Выполняет кластеризацию данных.
        /// </summary>
        /// <param name="data">Массив данных, каждая строка — вектор признаков (RGB)</param>
        /// <param name="k">количество кластеров</param>
        /// <returns>Массив меток кластеров для каждой точки данных (пикселя)</returns>
        int[] Cluster(double[][] data, int k);
    }
}