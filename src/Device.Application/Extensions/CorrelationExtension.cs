using System;

namespace Device.ApplicationExtension.Extension
{
    public static class CorrelationExtension
    {
        ///<summary>
        ///alike Accord.Statistics.Measures.Correlation.
        ///<para>http://accord-framework.net/docs/html/M_Accord_Statistics_Measures_Correlation_2.htm</para>
        ///</summary>
        public static double[][] Correlation(this double[][] matrix)
        {
            double[] means = Mean(matrix, dimension: 0);
            return Correlation(matrix, means, StandardDeviation(matrix, means));
        }

        private static double[][] Correlation(this double[][] matrix, double[] means, double[] standardDeviations)
        {
            int rows = matrix.Rows();
            int cols = matrix.Columns();

            double N = rows;
            double[][] cor = Zeros(cols, cols);
            for (int i = 0; i < cols; i++)
            {
                for (int j = i; j < cols; j++)
                {
                    double c = 0.0;
                    for (int k = 0; k < matrix.Length; k++)
                    {
                        double a = z(matrix[k][j], means[j], standardDeviations[j]);
                        double b = z(matrix[k][i], means[i], standardDeviations[i]);
                        c += a * b;
                    }
                    c /= N - 1.0;
                    cor[i][j] = c;
                    cor[j][i] = c;
                }
            }

            return cor;
        }

        private static double[][] Zeros(int rows, int columns)
        {
            return Zeros<double>(rows, columns);
        }

        private static T[][] Zeros<T>(int rows, int columns)
        {
            T[][] matrix = new T[rows][];
            for (int i = 0; i < matrix.Length; i++)
                matrix[i] = new T[columns];
            return matrix;
        }

        private static int Rows<T>(this T[] vector)
        {
            return vector.Length;
        }

        public static int Columns<T>(this T[][] matrix)
        {
            if (matrix.Length == 0)
                return 0;
            return matrix[0].Length;
        }

        private static double z(double v, double mean, double sdev)
        {
            if (sdev == 0.0)
            {
                sdev = 1E-12;
            }

            return (v - mean) / sdev;
        }

        private static double[] StandardDeviation(this double[][] matrix, double[] means, bool unbiased = true)
        {
            return matrix.Variance(means, unbiased).Sqrt();
        }

        private static double[] Sqrt(this double[] value)
        {
            return value.Sqrt(new double[value.Length]);
        }

        private static double[] Sqrt(this double[] value, double[] result)
        {
            for (int i = 0; i < value.Length; i++)
            {
                double num = value[i];
                result[i] = System.Math.Sqrt(num);
            }

            return result;
        }

        private static double[] Variance(this double[][] matrix, double[] means, bool unbiased = true)
        {
            int num = matrix.Length;
            if (num == 0)
            {
                return new double[0];
            }

            int num2 = matrix[0].Length;
            double num3 = num;
            double[] array = new double[num2];
            for (int i = 0; i < num2; i++)
            {
                double num4 = 0.0;
                double num5 = 0.0;
                double num6 = 0.0;
                for (int j = 0; j < num; j++)
                {
                    num6 = matrix[j][i] - means[i];
                    num4 += num6;
                    num5 += num6 * num6;
                }

                if (unbiased)
                {
                    array[i] = (num5 - num4 * num4 / num3) / (num3 - 1.0);
                }
                else
                {
                    array[i] = (num5 - num4 * num4 / num3) / num3;
                }
            }

            return array;
        }

        private static double[] Mean(this double[][] matrix, int dimension)
        {
            int num = matrix.Length;
            if (num == 0)
            {
                return new double[0];
            }

            double[] array;
            switch (dimension)
            {
                case 0:
                    {
                        int num2 = matrix[0].Length;
                        array = new double[num2];
                        double num3 = num;
                        for (int k = 0; k < num2; k++)
                        {
                            for (int l = 0; l < num; l++)
                            {
                                array[k] += matrix[l][k];
                            }
                            array[k] /= num3;
                        }

                        break;
                    }
                case 1:
                    {
                        array = new double[num];
                        for (int i = 0; i < num; i++)
                        {
                            for (int j = 0; j < matrix[i].Length; j++)
                            {
                                array[i] += matrix[i][j];
                            }
                            array[i] /= matrix[i].Length;
                        }

                        break;
                    }
                default:
                    throw new ArgumentException("Invalid dimension.", "dimension");
            }
            return array;
        }
    }
}
