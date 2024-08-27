using System.Text.RegularExpressions;

namespace AHI.Device.Function.Model.ImportModel.Validation
{
    public static class StringRegex
    {
        public static readonly Regex NUMBER = new Regex(@"^-?[0-9]+$", RegexOptions.Compiled);
        public static readonly Regex DOUBLE = new Regex(@"^-?[0-9]+(\.[0-9]+)?$", RegexOptions.Compiled);
        
        public const string EXPONENT_GROUP = "exponent";
        public static readonly Regex E_NOTATION = new Regex(@$"^-?[0-9]+(\.[0-9]+)?(?<{EXPONENT_GROUP}>E[+-]?[0-9]+)?$", RegexOptions.Compiled);
    }
}