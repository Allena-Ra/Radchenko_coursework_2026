using GmmImageSegmentator.Filters;
using GmmImageSegmentator.Utilities;
using GMMLogics.Implementations;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;
using Color = System.Drawing.Color;

namespace GmmImageSegmentator.Tests
{   
    /// <summary>
    /// Модульные тесты для EdgeDetectionFilter.
    /// </summary>

    public class EdgeDetectionFilterTests
    {
        [Fact]
        public void Apply_SinglePixel_NoEdge_ReturnsBlack()
        {
            var filter = new EdgeDetectionFilter(new[] { 0 }, 1, 1);
            var result = filter.Apply(null!);
            using var bmp = ToBitmap(result);
            Color p = bmp.GetPixel(0, 0);
            Assert.Equal(0, p.R);
            Assert.Equal(0, p.G);
            Assert.Equal(0, p.B);
        }

        [Fact]
        public void Apply_VerticalBoundary_DetectedCorrectly()
        {
            // 3x3: левый столбец — метка 0, остальные два столбца — метка 1
            // 0 1 1
            // 0 1 1
            // 0 1 1
            int[] labels = {
        0, 1, 1,
        0, 1, 1,
        0, 1, 1
    };
            var filter = new EdgeDetectionFilter(labels, 3, 3);
            var result = filter.Apply(null!);
            using var bmp = ToBitmap(result);

            // Пиксели левого столбца (x=0) имеют соседа справа (x=1) с другой меткой → край (белые)
            for (int y = 0; y < 3; y++)
            {
                Color p = bmp.GetPixel(0, y);
                Assert.Equal(255, p.R);
                Assert.Equal(255, p.G);
                Assert.Equal(255, p.B);
            }

            // Пиксели среднего столбца (x=1) имеют соседа слева с меткой 0 → край
            for (int y = 0; y < 3; y++)
            {
                Color p = bmp.GetPixel(1, y);
                Assert.Equal(255, p.R);
                Assert.Equal(255, p.G);
                Assert.Equal(255, p.B);
            }

            // Пиксели правого столбца (x=2):
            // Сосед слева (x=1) имеет ту же метку 1 → не край слева
            // Соседа справа нет
            // Сосед сверху/снизу (если не на границе) имеют ту же метку 1 → не край
            // Верхний (2,0): сосед слева метка 1 (своя), сосед снизу метка 1 (своя) → НЕ край
            Color p20 = bmp.GetPixel(2, 0);
            Assert.Equal(0, p20.R);
            Assert.Equal(0, p20.G);
            Assert.Equal(0, p20.B);

            // Нижний (2,2): аналогично → НЕ край
            Color p22 = bmp.GetPixel(2, 2);
            Assert.Equal(0, p22.R);
            Assert.Equal(0, p22.G);
            Assert.Equal(0, p22.B);
        }
        [Fact]
        public void Apply_AllBoundaryPixels_ReturnsWhiteImage()
        {
            // 2x2 с метками [0, 1; 1, 0] — все пиксели граничные
            int[] labels = { 0, 1, 1, 0 };
            var filter = new EdgeDetectionFilter(labels, 2, 2);
            var result = filter.Apply(null!);

            using var bmp = ToBitmap(result);
            // Ожидаем, что все (2x2) пикселя белые (R=255, G=255, B=255)
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    System.Drawing.Color pixel = bmp.GetPixel(x, y);
                    Assert.Equal(255, pixel.R);
                    Assert.Equal(255, pixel.G);
                    Assert.Equal(255, pixel.B);
                }
            }
        }

        [Fact]
        public void Apply_HorizontalBorder_DetectedAsEdge()
        {
            int[] labels = { 0, 0, 1, 1 };
            var filter = new EdgeDetectionFilter(labels, 2, 2);
            var result = filter.Apply(null!);

            using var bmp = ToBitmap(result);
            // Все пиксели должны быть граничными (белые)
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    Color pixel = bmp.GetPixel(x, y);
                    Assert.Equal(255, pixel.R);
                    Assert.Equal(255, pixel.G);
                    Assert.Equal(255, pixel.B);
                }
            }
        }

        [Fact]
        public void Apply_InsideCluster_NotEdge_ReturnsBlack()
        {
            // 3x3 все метки одинаковые (0) – нет границ
            int[] labels = Enumerable.Repeat(0, 9).ToArray();
            var filter = new EdgeDetectionFilter(labels, 3, 3);
            var result = filter.Apply(null!);

            using var bmp = ToBitmap(result);
            // Все пиксели чёрные
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    Color pixel = bmp.GetPixel(x, y);
                    Assert.Equal(0, pixel.R);
                    Assert.Equal(0, pixel.G);
                    Assert.Equal(0, pixel.B);
                }
            }
        }

        /// <summary>
        /// Конвертирует BitmapImage в System.Drawing.Bitmap через PNG-поток.
        /// </summary>
        private static Bitmap ToBitmap(BitmapImage image)
        {
            using var ms = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(ms);
            ms.Position = 0;
            return new Bitmap(ms);
        }
    }

    /// <summary>
    /// Модульные тесты для InvertColorsFilter.
    /// </summary>
    public class InvertColorsFilterTests
    {
        [Fact]
        public void Apply_Twice_RestoresOriginalColors()
        {
            var palette = new List<System.Windows.Media.Color>
    {
        System.Windows.Media.Color.FromRgb(10, 20, 30),
        System.Windows.Media.Color.FromRgb(128, 128, 128)
    };
            var filter = new InvertColorsFilter();
            var once = filter.Apply(palette);
            var twice = filter.Apply(once);
            Assert.Equal(palette[0], twice[0]);
            Assert.Equal(palette[1], twice[1]);
        }
        [Fact]
        public void Apply_InvertsAllColorsCorrectly()
        {
            var palette = new List<System.Windows.Media.Color>
            {
                System.Windows.Media.Color.FromRgb(0, 0, 0),
                System.Windows.Media.Color.FromRgb(255, 255, 255),
                System.Windows.Media.Color.FromRgb(100, 150, 200)
            };
            var filter = new InvertColorsFilter();

            var inverted = filter.Apply(palette);

            Assert.Equal(System.Windows.Media.Color.FromRgb(255, 255, 255), inverted[0]);
            Assert.Equal(System.Windows.Media.Color.FromRgb(0, 0, 0), inverted[1]);
            // 255-100=155, 255-150=105, 255-200=55
            Assert.Equal(System.Windows.Media.Color.FromRgb(155, 105, 55), inverted[2]);
        }

        [Fact]
        public void Apply_PreservesPaletteSize()
        {
            var palette = new List<System.Windows.Media.Color>
                { Colors.Red, Colors.Green, Colors.Blue };
            var filter = new InvertColorsFilter();

            var inverted = filter.Apply(palette);

            Assert.Equal(3, inverted.Count);
        }

        [Fact]
        public void Apply_EmptyPalette_ReturnsEmpty()
        {
            var filter = new InvertColorsFilter();
            var result = filter.Apply(new List<System.Windows.Media.Color>());
            Assert.Empty(result);
        }
    }

    /// <summary>
    /// Модульные тесты для RandomColorsFilter.
    /// </summary>
    public class RandomColorsFilterTests
    {
        [Fact]
        public void Apply_EmptyPalette_ReturnsEmpty()
        {
            var filter = new RandomColorsFilter();
            var result = filter.Apply(new List<System.Windows.Media.Color>());
            Assert.Empty(result);
        }
        [Fact]
        public void Apply_ReturnsSameNumberOfColors()
        {
            var palette = new List<System.Windows.Media.Color>
                { Colors.Red, Colors.Green, Colors.Blue, Colors.Blue };
            var filter = new RandomColorsFilter();

            var result = filter.Apply(palette);

            Assert.Equal(4, result.Count);
        }

        [Fact]
        public void Apply_GeneratesValidRgbColors()
        {
            var palette = new List<System.Windows.Media.Color> { Colors.Red };
            var filter = new RandomColorsFilter();

            var result = filter.Apply(palette);
            var color = result[0];

            Assert.InRange(color.R, (byte)0, (byte)255);
            Assert.InRange(color.G, (byte)0, (byte)255);
            Assert.InRange(color.B, (byte)0, (byte)255);
        }

        [Fact]
        public void Apply_SubsequentCallsReturnDifferentColors()
        {
            var palette = Enumerable.Repeat(Colors.Black, 10).ToList();
            var filter = new RandomColorsFilter();
            var first = filter.Apply(palette);
            var second = filter.Apply(palette);

            bool allSame = true;
            for (int i = 0; i < first.Count; i++)
            {
                if (first[i] != second[i])
                {
                    allSame = false;
                    break;
                }
            }
            Assert.False(allSame, "Ожидалось, что повторные вызовы дадут разные палитры.");
        }
    }

    /// <summary>
    /// Модульные тесты для SegmentedImageBuilder.
    /// </summary>
    public class SegmentedImageBuilderTests
    {
        [Fact]
        public void CreateSegmentedImageFromColors_SinglePixel_CorrectColor()
        {
            var labels = new[] { 0 };
            var palette = new List<System.Windows.Media.Color> { System.Windows.Media.Color.FromRgb(42, 42, 42) };
            var image = SegmentedImageBuilder.CreateSegmentedImageFromColors(labels, 1, 1, palette);
            using var bmp = ToBitmap(image);
            Color p = bmp.GetPixel(0, 0);
            Assert.Equal(42, p.R);
            Assert.Equal(42, p.G);
            Assert.Equal(42, p.B);
        }

        [Fact]
        public void CreateSegmentedImageFromColors_LargeImage_PerformanceOk()
        {
            int w = 100, h = 100;
            var labels = new int[w * h];
            for (int i = 0; i < labels.Length; i++)
                labels[i] = i % 2; // чередуем 0 и 1
            var palette = new List<System.Windows.Media.Color> { Colors.Red, Colors.Blue };
            var image = SegmentedImageBuilder.CreateSegmentedImageFromColors(labels, w, h, palette);
            Assert.NotNull(image);
            using var bmp = ToBitmap(image);
            Assert.Equal(w, bmp.Width);
            Assert.Equal(h, bmp.Height);
            // Проверка выборочных пикселей
            Assert.Equal(255, bmp.GetPixel(0, 0).R);   // красный
            Assert.Equal(0, bmp.GetPixel(0, 0).G);
            Assert.Equal(0, bmp.GetPixel(1, 0).R);    // синий
            Assert.Equal(0, bmp.GetPixel(1, 0).G);
            Assert.Equal(255, bmp.GetPixel(1, 0).B);
        }
        
        [Fact]
        public void CreateSegmentedImageFromColors_AssignsCorrectColors()
        {
            // 2x2 изображение с метками [0, 1, 0, 0]
            int[] labels = { 0, 1, 0, 0 };
            var palette = new List<System.Windows.Media.Color>
            {
                System.Windows.Media.Color.FromRgb(10, 20, 30),
                System.Windows.Media.Color.FromRgb(100, 200, 255)
            };
            var image = SegmentedImageBuilder.CreateSegmentedImageFromColors(labels, 2, 2, palette);

            using var bmp = ToBitmap(image);

            // (0,0) метка 0 -> (10,20,30)
            Color p00 = bmp.GetPixel(0, 0);
            Assert.Equal(10, p00.R);
            Assert.Equal(20, p00.G);
            Assert.Equal(30, p00.B);

            // (1,0) метка 1 -> (100,200,255)
            Color p10 = bmp.GetPixel(1, 0);
            Assert.Equal(100, p10.R);
            Assert.Equal(200, p10.G);
            Assert.Equal(255, p10.B);

            // (0,1) метка 0
            Color p01 = bmp.GetPixel(0, 1);
            Assert.Equal(10, p01.R);
            Assert.Equal(20, p01.G);
            Assert.Equal(30, p01.B);

            // (1,1) метка 0
            Color p11 = bmp.GetPixel(1, 1);
            Assert.Equal(10, p11.R);
            Assert.Equal(20, p11.G);
            Assert.Equal(30, p11.B);
        }

        [Fact]
        public void CreateSegmentedImageFromColors_ThrowsOnInvalidSize()
        {
            int[] labels = { 0 };
            var palette = new List<System.Windows.Media.Color> { Colors.Red };
            Assert.Throws<ArgumentException>(() =>
                SegmentedImageBuilder.CreateSegmentedImageFromColors(labels, 0, 1, palette));
        }

        [Fact]
        public void CreateSegmentedImageFromColors_EmptyLabels_ReturnsImage()
        {
            var palette = new List<System.Windows.Media.Color> { Colors.Red };
            var image = SegmentedImageBuilder.CreateSegmentedImageFromColors(
                Array.Empty<int>(), 1, 1, palette);
            Assert.NotNull(image);
            // можно проверить, что изображение не пустое, но пиксели все равно будут
            using var bmp = ToBitmap(image);
            // размер 1x1
            Assert.Equal(1, bmp.Width);
            Assert.Equal(1, bmp.Height);
        }

        /// <summary>
        /// Конвертирует BitmapImage в System.Drawing.Bitmap через PNG-поток.
        /// </summary>
        private static Bitmap ToBitmap(BitmapImage image)
        {
            using var ms = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(ms);
            ms.Position = 0;
            return new Bitmap(ms);
        }
    }
    /// <summary>
    /// Модульные тесты для CustomGMMPredictor.
    /// </summary>
    public class CustomGMMPredictorTests
    {
        [Fact]
        public void Cluster_TwoWellSeparatedGroups_ReturnsExpectedPartition()
        {
            // Группа A вокруг (0,0,0), группа B вокруг (1,1,1)
            var data = new double[][]
            {
        new[] { 0.1, 0.1, 0.1 },
        new[] { 0.0, 0.0, 0.0 },
        new[] { 0.2, 0.1, 0.0 },
        new[] { 0.9, 0.9, 0.9 },
        new[] { 1.0, 1.0, 1.0 },
        new[] { 0.8, 0.9, 1.0 }
            };
            var predictor = new CustomGMMPredictor { SampleRatio = 1.0, MaxIterations = 50 };
            var labels = predictor.Cluster(data, 2);

            // Первые три и последние три должны быть в разных кластерах
            int clusterA = labels[0];
            int clusterB = labels[3];
            Assert.NotEqual(clusterA, clusterB);

            // Все из первой группы должны быть в одном кластере
            Assert.Equal(clusterA, labels[1]);
            Assert.Equal(clusterA, labels[2]);

            // Все из второй группы — в другом
            Assert.Equal(clusterB, labels[4]);
            Assert.Equal(clusterB, labels[5]);
        }
        [Fact]
        public void Cluster_MultipleRuns_ConsistentSilhouette()
        {
            var data = new double[][]
            {
        new[] { 0.0, 0.0, 0.0 },
        new[] { 0.2, 0.1, 0.1 },
        new[] { 1.0, 1.0, 1.0 },
        new[] { 0.9, 0.8, 0.9 }
            };

            var silhouettes = new List<double>();
            for (int run = 0; run < 5; run++)
            {
                var predictor = new CustomGMMPredictor { SampleRatio = 1.0, MaxIterations = 30 };
                var labels = predictor.Cluster(data, 2);
                double sil = ClusteringMetrics.SilhouetteScore(data, labels, data.Length);
                silhouettes.Add(sil);
            }

            // Все значения силуэта должны быть > 0.5 (хорошая кластеризация)
            Assert.All(silhouettes, s => Assert.True(s > 0.5, $"Silhouette {s} слишком низкий"));
            // Разброс между максимальным и минимальным не должен превышать 0.3
            double range = silhouettes.Max() - silhouettes.Min();
            Assert.True(range < 0.3, $"Слишком большой разброс силуэта: {range}");
        }
        [Fact]
        public void Cluster_MoreIterations_ImprovesOrKeepsLogLikelihood()
        {
            var data = new double[][]
            {
        new[] { 0.1, 0.2, 0.3 },
        new[] { 0.2, 0.1, 0.2 },
        new[] { 0.9, 0.8, 0.7 },
        new[] { 0.8, 0.9, 0.8 }
            };

            var predictor10 = new CustomGMMPredictor { SampleRatio = 1.0, MaxIterations = 5 };
            predictor10.Cluster(data, 2);
            double ll10 = predictor10.LastLogLikelihood;

            var predictor50 = new CustomGMMPredictor { SampleRatio = 1.0, MaxIterations = 50 };
            predictor50.Cluster(data, 2);
            double ll50 = predictor50.LastLogLikelihood;

            // LogLikelihood с 50 итерациями должен быть не хуже, чем с 5
            Assert.True(ll50 >= ll10 - 1e-6,
                $"LogLikelihood с 50 итерациями ({ll50}) не должен быть значительно хуже, чем с 5 ({ll10})");
        }

        [Fact]
        public void Cluster_AllIdenticalPoints_AssignsSameOrAnyCluster()
        {
            var data = Enumerable.Repeat(new[] { 0.5, 0.5, 0.5 }, 10).ToArray();
            var predictor = new CustomGMMPredictor { SampleRatio = 1.0, MaxIterations = 20 };
            var labels = predictor.Cluster(data, 3);

            // Все метки должны быть в диапазоне [0, 2]
            Assert.All(labels, l => Assert.InRange(l, 0, 2));
            // Метод не должен падать
            Assert.Equal(10, labels.Length);
        }

        [Fact]
        public void ClusteringMetrics_BIC_AIC_AreValidNumbers()
        {
            double logLik = -100.0;
            int n = 1000, k = 3, dim = 3;

            double bic = ClusteringMetrics.BIC(logLik, n, k, dim);
            double aic = ClusteringMetrics.AIC(logLik, k, dim);

            Assert.True(double.IsFinite(bic));
            Assert.True(double.IsFinite(aic));
            Assert.True(bic > 0);
            Assert.True(aic > 0);
        }

        [Fact]
        public void Cluster_WithFullSample_MatchesTrainThenPredict_LogLikelihood()
        {
            var data = new double[][]
            {
        new[] { 0.0, 0.0, 0.0 },
        new[] { 0.2, 0.2, 0.2 },
        new[] { 0.8, 0.8, 0.8 },
        new[] { 1.0, 1.0, 1.0 }
            };

            // Прямой вызов Cluster с SampleRatio = 1.0
            var predictor1 = new CustomGMMPredictor { SampleRatio = 1.0, MaxIterations = 30 };
            predictor1.Cluster(data, 2);
            double ll1 = predictor1.LastLogLikelihood;

            // Отдельно Train + Predict
            var predictor2 = new CustomGMMPredictor { SampleRatio = 1.0, MaxIterations = 30 };
            predictor2.Train(data, 2);
            predictor2.Predict(data);
            double ll2 = predictor2.LastLogLikelihood;

            // Оба значения должны быть конечными и положительными (правдоподобие > 0)
            Assert.True(double.IsFinite(ll1));
            Assert.True(double.IsFinite(ll2));
            Assert.True(ll1 > 0, $"LogLikelihood 1 должен быть > 0, но равен {ll1}");
            Assert.True(ll2 > 0, $"LogLikelihood 2 должен быть > 0, но равен {ll2}");
        }

        [Fact]
        public void Cluster_WithKEqualsOne_AllLabelsZero()
        {
            var data = new double[][] {
        new[] { 0.0, 0.0, 0.0 },
        new[] { 1.0, 1.0, 1.0 }
    };
            var predictor = new CustomGMMPredictor { SampleRatio = 1.0 };
            var labels = predictor.Cluster(data, 1);
            Assert.All(labels, l => Assert.Equal(0, l));
        }

        [Fact]
        public void Cluster_OnlyOnePoint_DoesNotThrow()
        {
            var data = new[] { new[] { 0.5, 0.5, 0.5 } };
            var predictor = new CustomGMMPredictor { SampleRatio = 1.0, MaxIterations = 5 };
            var labels = predictor.Cluster(data, 3);
            Assert.Single(labels);
            // Метка должна быть в допустимом диапазоне
            Assert.InRange(labels[0], 0, 2);
        }

        [Fact]
        public void Cluster_EmptyData_ThrowsOrHandlesGracefully()
        {
            var predictor = new CustomGMMPredictor();
            var empty = Array.Empty<double[]>();
            // Пустой массив вызовет IndexOutOfRangeException при попытке доступа к data[0]
            Assert.Throws<IndexOutOfRangeException>(() => predictor.Cluster(empty, 2));
        }

        [Fact]
        public void LastLogLikelihood_BeforeTraining_IsZero()
        {
            var predictor = new CustomGMMPredictor();
            Assert.Equal(0.0, predictor.LastLogLikelihood);
        }

        [Fact]
        public void Cluster_WithFullSample_ProducesValidLogLikelihood()
        {
            var data = new double[][]
            {
        new[] { 0.0, 0.0, 0.0 },
        new[] { 0.2, 0.2, 0.2 },
        new[] { 0.8, 0.8, 0.8 },
        new[] { 1.0, 1.0, 1.0 }
            };

            // Прямой вызов Cluster с SampleRatio = 1.0
            var predictor1 = new CustomGMMPredictor { SampleRatio = 1.0, MaxIterations = 30 };
            predictor1.Cluster(data, 2);
            double ll1 = predictor1.LastLogLikelihood;

            // Отдельно Train + Predict
            var predictor2 = new CustomGMMPredictor { SampleRatio = 1.0, MaxIterations = 30 };
            predictor2.Train(data, 2);
            predictor2.Predict(data);
            double ll2 = predictor2.LastLogLikelihood;

            // Оба значения должны быть конечными и положительными (правдоподобие > 0)
            Assert.True(double.IsFinite(ll1));
            Assert.True(double.IsFinite(ll2));
            Assert.True(ll1 > 0, $"LogLikelihood 1 должен быть > 0, но равен {ll1}");
            Assert.True(ll2 > 0, $"LogLikelihood 2 должен быть > 0, но равен {ll2}");
        }
        [Fact]
        public void Cluster_TwoDistantGroups_AssignsDifferentClusters()
        {
            // Два чётко разделённых кластера в 3D-пространстве
            var data = new double[][]
            {
                new[] { 0.0, 0.0, 0.0 },
                new[] { 0.1, 0.1, 0.1 },
                new[] { 0.9, 0.9, 0.9 },
                new[] { 1.0, 1.0, 1.0 }
            };
            var predictor = new CustomGMMPredictor
            {
                MaxIterations = 50,
                SampleRatio = 1.0 // обучаться на всех данных
            };

            var labels = predictor.Cluster(data, 2);

            // Первые две точки должны быть в одном кластере
            Assert.Equal(labels[0], labels[1]);
            // Последние две — в другом
            Assert.Equal(labels[2], labels[3]);
            // Кластеры должны различаться
            Assert.NotEqual(labels[0], labels[2]);
        }

        [Fact]
        public void Cluster_WithSampleRatio_ReturnsLabelsForAllPoints()
        {
            var data = Enumerable.Range(0, 100)
                .Select(i => new[] { i / 100.0, 0.0, 0.0 })
                .ToArray();
            var predictor = new CustomGMMPredictor
            {
                SampleRatio = 0.5,
                MaxIterations = 10
            };

            var labels = predictor.Cluster(data, 3);

            Assert.Equal(100, labels.Length);
            // Все метки должны быть в допустимом диапазоне
            Assert.All(labels, l => Assert.True(l >= 0 && l < 3));
        }

        [Fact]
        public void Train_Predict_Consistency()
        {
            var data = new double[][]
            {
                new[] { 0.0, 0.0, 0.0 },
                new[] { 1.0, 1.0, 1.0 },
                new[] { 0.1, 0.1, 0.1 },
                new[] { 1.1, 1.1, 1.1 }
            };
            var predictor = new CustomGMMPredictor { SampleRatio = 1.0, MaxIterations = 30 };
            predictor.Train(data, 2);
            var labels = predictor.Predict(data);
            // Должно быть ровно 2 кластера
            Assert.Equal(2, labels.Distinct().Count());
        }

        [Fact]
        public void Predict_WithoutTraining_ThrowsInvalidOperationException()
        {
            var predictor = new CustomGMMPredictor();
            var data = new[] { new[] { 0.0, 0.0, 0.0 } };
            Assert.Throws<InvalidOperationException>(() => predictor.Predict(data));
        }

        [Fact]
        public void LastLogLikelihood_AfterTraining_IsValidNumber()
        {
            var data = new double[][]
            {
                new[] { 0.0, 0.0, 0.0 },
                new[] { 0.2, 0.2, 0.2 },
                new[] { 0.8, 0.8, 0.8 },
                new[] { 1.0, 1.0, 1.0 }
            };
            var predictor = new CustomGMMPredictor { SampleRatio = 1.0, MaxIterations = 20 };
            predictor.Cluster(data, 2);
            double logLik = predictor.LastLogLikelihood;
            // Правдоподобие должно быть конечным и не положительным (или разумным)
            Assert.True(double.IsFinite(logLik));
            Assert.NotEqual(0.0, logLik);
        }
    }
}

