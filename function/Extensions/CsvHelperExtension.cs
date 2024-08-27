using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using System.Linq;

namespace Function.Extension
{
    public static class CsvHelperExtension
    {
        public static CsvReader CreateCsvHelper(StreamReader reader)
        {
            var configuration = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HeaderValidated = null,
                MissingFieldFound = null,
                HasHeaderRecord = false,
                IgnoreBlankLines = true,
                ShouldSkipRecord = args => args.Row.Parser.Record.All(f => string.IsNullOrWhiteSpace(f))
            };
            return new CsvReader(reader, configuration);
        }
    }
}
