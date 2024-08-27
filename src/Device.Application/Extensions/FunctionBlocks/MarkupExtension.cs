using System.Text.RegularExpressions;

namespace Device.Application.Service
{
    public static class MarkupHelper
    {
        private const string MARKUP_TARGET_PATTERN = @"^((?:""[^""]*""|\w+)\.)?(?:""([^""]*)""|(\w+))$";
        private const int INDEX_GROUP_MARKUP_NAME = 1; // ABC.DEF => ABC. in Group 1 | "ANC.DEF".XYZ => "ANC.DEF". in group 1
        private const int INDEX_GROUP_TARGET_NAME_WITH_QOUTE = 2; // HBC."DEF.XYZ" => Has quote, DEF.XYZ in Group 2
        private const int INDEX_GROUP_MARKUP_NAME_WITHOUT_QOUTE = 3; // ABC.DEF => Wihout quote, DEF in Group 3

        private static (string MarkupName, string TargetName) SplitSourceToMarkupTarget(string input)
        {
            Regex regex = new Regex(MARKUP_TARGET_PATTERN);
            Match match = regex.Match(input);

            if (match.Success)
            {
                string marupName = match.Groups[INDEX_GROUP_MARKUP_NAME].Success
                                    ? match.Groups[INDEX_GROUP_MARKUP_NAME].Value
                                    : string.Empty;
                string targetName = match.Groups[INDEX_GROUP_TARGET_NAME_WITH_QOUTE].Success
                                    ? match.Groups[INDEX_GROUP_TARGET_NAME_WITH_QOUTE].Value
                                    : match.Groups[INDEX_GROUP_MARKUP_NAME_WITHOUT_QOUTE].Value;
                marupName = marupName.TrimEnd('.').Replace("\"", "");

                return (marupName.Trim(), targetName.Trim());
            }
            return (null, null);
        }

        public static string GetMarkupName(this string input)
        {
            return SplitSourceToMarkupTarget(input).MarkupName;
        }
        public static string GetTargetName(this string input)
        {
            return SplitSourceToMarkupTarget(input).TargetName;
        }
    }
}