using System.Collections.Generic;

namespace AHI.Device.Function.FileParser.Template.Constant
{
    static class MetadataIndexMapping
    {
        static readonly IDictionary<string, int> _propertyIndexMapping = new Dictionary<string, int> {
            {"TemplateType", 0},
            {"SheetNameProperty", 1},
            {"ObjectChildGap", 2}
        };

        public static int GetIndex(string propertyName) => _propertyIndexMapping[propertyName];
    }
}