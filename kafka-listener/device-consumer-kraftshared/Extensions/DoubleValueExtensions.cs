using System;
using System.Collections.Generic;
using System.Linq;

namespace Device.Consumer.KraftShared.Extensions
{
    public static class DoubleValueExtensions
    {
        private static readonly string _toStringFormat = "0.#################";
        private static readonly double[] _precisions = new double[]
        {
            1d, 0.1d, 0.01d, 0.001d, 0.0001d, 0.00001d, 0.000001d, 0.0000001d, 0.00000001d, 0.000000001d,
            0.0000000001d, 0.00000000001d, 0.000000000001d, 0.0000000000001d, 0.00000000000001d, 0.000000000000001d
        };

        // return whether the double value represent an integer, with precision ~ 0.5 * 10^-k
        public static bool IsInteger(this double n, int k)
        {
            var r = Math.Round(n % 1, k);
            var d = Math.Abs(Math.Round(r) - r);
            var p = _precisions[k];
            return Math.Round(d, k) < p;
        }

        public static string ToNumberString(this double n)
        {
            var result = n.ToString();
            if(result.Contains('E', StringComparison.InvariantCultureIgnoreCase)) result = n.ToString(_toStringFormat);
            return result;
        }

        public static string ToNumberString(this double? n) => n.HasValue ? n.Value.ToNumberString() : null;

        // Calculate UoM
        public static double Multiply(params double[] numbers)
        {
            if (!numbers.Any())
            {
                throw new ArgumentNullException();
            }

            if (numbers.Length == 1)
                return numbers[0];

            return TryConvertToDecimalList(numbers, out var decimals) ? MultiplyAsDecimal(decimals) : MultiplyAsDouble(numbers);
        }

        private static double MultiplyAsDecimal(IEnumerable<decimal> numbers)
        {
            try
            {
                return (double)numbers.Aggregate(1m, (s, n) => s * n);
            }
            catch (OverflowException)
            {
                // result is out of range for decimal multiply, fall back to double multiply
                return MultiplyAsDouble(numbers.Select(number => (double)number));
            }
        }

        private static double MultiplyAsDouble(IEnumerable<double> numbers)
        {
            return numbers.Aggregate(1d, (s, n) => s * n);
        }

        private static bool TryConvertToDecimalList(IEnumerable<double> doubles, out IList<decimal> decimals)
        {
            try
            {
                decimals = doubles.Select(number => (decimal)number).ToList();
                return true;
            }
            catch(OverflowException)
            {
                decimals = null;
                return false;
            }
        }
    }
}