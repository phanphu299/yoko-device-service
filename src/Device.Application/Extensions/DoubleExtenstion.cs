using System;
using System.Collections.Generic;
using System.Linq;

namespace Device.ApplicationExtension.Extension
{
    public static class DoubleExtension
    {
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
