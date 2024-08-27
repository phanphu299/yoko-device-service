using System;
using System.Numerics;
using Device.Consumer.KraftShared.Constant;

namespace Device.Consumer.KraftShared.Extensions
{
    public static class DynamicValueExtensions
    {
        public static void ValidateType(this object value, string type)
        {
            switch (type)
            {
                case DataTypeConstants.TYPE_BOOLEAN:
                    ValidateBoolean(value);
                    break;
                case DataTypeConstants.TYPE_TEXT:
                    ValidateText(value);
                    break;
                case DataTypeConstants.TYPE_INTEGER:
                    ValidateInteger(value);
                    break;
                case DataTypeConstants.TYPE_DOUBLE:
                    ValidateDouble(value);
                    break;
            }
        }

        private static void ValidateBoolean(object value)
        {
            if (value is bool)
                return;
            throw new FormatException();
        }

        private static void ValidateText(object value)
        {
            if (value is string)
                return;
            throw new FormatException();
        }

        private static void ValidateInteger(object value)
        {
            bool isInRange;
            switch (value)
            {
                case int:
                case short:
                case byte:
                    isInRange = true;
                    break;
                case long:
                    isInRange = int.MinValue <= (long)value && (long)value <= int.MaxValue;
                    break;
                case decimal:
                case double:
                    isInRange = int.MinValue <= (double)value && (double)value <= int.MaxValue;
                    break;
                case BigInteger:
                    isInRange = int.MinValue <= (BigInteger)value && (BigInteger)value <= int.MaxValue;
                    break;
                default:
                    throw new FormatException();
            }
            if (!isInRange)
                throw new OverflowException();
        }

        private static void ValidateDouble(object value)
        {
            bool isInRange;
            switch (value)
            {
                case int:
                case short:
                case byte:
                case long:
                    isInRange = true;
                    break;
                case decimal:
                case double:
                    isInRange = double.MinValue <= (double)value && (double)value <= double.MaxValue;
                    break;
                case BigInteger:
                    isInRange = (BigInteger)double.MinValue <= (BigInteger)value && (BigInteger)value <= (BigInteger)double.MaxValue;
                    break;
                default:
                    throw new FormatException();
            }
            if (!isInRange)
                throw new OverflowException();
        }

        public static object ConvertTo(this object value, string type)
        {
            // value.ValidateType(type);
            return type switch
            {
                DataTypeConstants.TYPE_BOOLEAN => (bool)value,
                DataTypeConstants.TYPE_TEXT => (string)value,
                DataTypeConstants.TYPE_INTEGER => CastToInteger(value),
                DataTypeConstants.TYPE_DOUBLE => CastToDouble(value),
                _ => value
            };
        }

        private static int CastToInteger(object value)
        {
            return value switch
            {
                byte byteValue => byteValue,
                short shortValue => shortValue,
                long longValue => (int)longValue,
                BigInteger bigIntegerValue => (int)bigIntegerValue,
                _ => (int)value,
            };
        }

        private static double CastToDouble(object value)
        {
            return value switch
            {
                byte byteValue => byteValue,
                short shortValue => shortValue,
                int intValue => intValue,
                long longValue => longValue,
                BigInteger bigIntegerValue => (double)bigIntegerValue,
                _ => (double)value,
            };
        }
    }
}
