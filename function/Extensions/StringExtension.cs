using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Model.ImportModel.Validation;
using JsonConstant = AHI.Infrastructure.SharedKernel.Extension.Constant;

namespace Function.Extension
{
    public static class StringExtension
    {
        public static string RemoveFileToken(this string fileName)
        {
            var index = fileName?.IndexOf("?token=") ?? -1;
            return index < 0 ? fileName ?? string.Empty : fileName.Remove(index);
        }

        public static bool TryParseUtcDatetime(this string dateTimeString, out DateTime dateTime)
        {
            dateTime = DateTime.MinValue;
            var format = JsonConstant.DefaultDateTimeFormat;
            var provider = CultureInfo.InvariantCulture;
            var style = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;

            bool success = DateTime.TryParseExact(dateTimeString, format, provider, style, out dateTime);
            if (!success)
                success = DateTime.TryParse(dateTimeString, provider, style, out dateTime);
            return success;
        }

        public static IEnumerable<string> SplitStringWithSpan(this string stringToSplit, char seperator = ',')
        {
            if (string.IsNullOrEmpty(stringToSplit))
                return Array.Empty<string>();

            ReadOnlySpan<char> span = stringToSplit.AsSpan();
            int nextSeperatorIndex = 0;
            int insertValAtIndex = 0;
            bool isLastLoop = false;
            List<string> result = new List<string>();
            while (!isLastLoop)
            {
                int indexStart = nextSeperatorIndex;
                nextSeperatorIndex = stringToSplit.IndexOf(seperator, indexStart);
                isLastLoop = (nextSeperatorIndex == -1);
                if (isLastLoop)
                {
                    nextSeperatorIndex = stringToSplit.Length;
                }

                ReadOnlySpan<char> slice = span.Slice(indexStart, nextSeperatorIndex - indexStart);
                string valParsed = slice.ToString();
                result.Add(valParsed);
                insertValAtIndex++;

                // Skip the seperator for next iteration
                nextSeperatorIndex++;
            }

            return result;
        }

        public static string ParseValue(this string value, string dataType, string dateTimeFormat)
        {
            switch (dataType)
            {
                case DataTypeConstants.TYPE_BOOLEAN:
                    return bool.Parse(value).ToString().ToLowerInvariant();
                case DataTypeConstants.TYPE_DOUBLE:
                    {
                        var match = StringRegex.E_NOTATION.Match(value);
                        if (!match.Success)
                            break;

                        var parse = double.TryParse(value, out double valueParse);
                        if (!parse || double.IsInfinity(valueParse))
                            throw new OverflowException();

                        var isNotation = !string.IsNullOrEmpty(match.Groups[StringRegex.EXPONENT_GROUP].Value);
                        return isNotation ? valueParse.ToNumberString() : value;
                    }
                case DataTypeConstants.TYPE_INTEGER: // choose integer data type but response return type is float is still true
                    // follow Bug 69812 : https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/69812/
                    if (double.TryParse(value.ToString(), out var intValue))
                    {
                        try
                        {

                            return Convert.ToInt32(intValue).ToString();
                        }
                        catch
                        {
                            throw new FormatException();
                        }
                    }
                    throw new FormatException();
                case DataTypeConstants.TYPE_TEXT:
                    if (Regex.IsMatch(value, "^(?=.{0,255}$)"))
                        return value;
                    var e = new ArgumentOutOfRangeException();
                    e.Data["MaxLength"] = 255;
                    throw e;
                case DataTypeConstants.TYPE_DATETIME:
                    var val = DateTime.ParseExact(value, dateTimeFormat, CultureInfo.InvariantCulture);
                    return val.ToString(dateTimeFormat);
            }
            throw new FormatException();
        }

        public static string GetCacheKey(this string cacheKey, params object[] args)
        {
            return string.Format(cacheKey, args);
        }

        public static string NormalizeSheetName(this string name)
        {
            const string invalidCharsRegex = @"[/\\*'?\[\]:]{1}";
            const int maxLength = 31;

            string safeName = Regex.Replace(name, invalidCharsRegex, "_")
                                    .Trim();

            if (safeName.Length > maxLength)
            {
                safeName = safeName.Substring(0, maxLength);
            }
            return safeName;
        }

        public static string EscapePattern(this string input)
        {
            return Regex.Replace(input, @$"[%_\\]", match => string.Format(@"\{0}", match.Value));
        }

        public static string PreProcessExpression(this string expression)
        {
            var pattern = @"\$\{(.*?)\}\$";
            var matches = Regex.Matches(expression.Trim(), pattern);
            foreach (Match m in matches)
            {
                expression = expression.Replace($"${{{m.Groups[1]}}}$", $"${{ {m.Groups[1].ToString().Trim()} }}$");
            }
            return expression;
        }

        public static string AppendReturn(this string expressionValidate)
        {
            if (!expressionValidate.Contains("return "))
            {
                expressionValidate = $"return {expressionValidate}";
            }
            return expressionValidate;
        }
        
        public static string CutOffFloatingPointPlace(this string timestamp)
        {
            if (double.TryParse(timestamp, out double unixDoubleTimestamp))
            {
                return Convert.ToInt64(unixDoubleTimestamp).ToString();
            }
            return timestamp;
        }
    }
}
