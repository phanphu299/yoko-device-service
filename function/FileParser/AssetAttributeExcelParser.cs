using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using AHI.Device.Function.Model.ImportModel;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Infrastructure.Import.Handler;
using AHI.Device.Function.Constant;
using static AHI.Device.Function.Constant.ErrorMessage;
using System.Linq;
using AHI.Device.Function.FileParser.ErrorTracking.Abstraction;

namespace AHI.Device.Function.FileParser
{
    public class AssetAttributeExcelParser : ExcelFileHandler<AssetAttribute>
    {
        private readonly IServiceProvider _provider;
        private readonly IParserContext _context;
        private ISheetParser<AssetAttribute> _parser;
        private readonly IExcelTrackingService _errorService;
        private readonly string[] REQUIRED_COLUMNS = { AssetAttributeHeader.ATTRIBUTE_NAME, AssetAttributeHeader.ATTRIBUTE_TYPE, AssetAttributeHeader.DATA_TYPE };

        public AssetAttributeExcelParser(IServiceProvider provider, IParserContext context, IExcelTrackingService errorService)
        {
            _provider = provider;
            _context = context;
            _errorService = errorService;
        }

        protected override IEnumerable<AssetAttribute> Parse(ISheet reader)
        {
            if (_parser is null)
                _parser = ISheetParser<AssetAttribute>.GetParser(_provider, _context);

            var result = _parser.ParseSheet(reader, true).ToList();
            var header = _parser.ParserTemplate.TemplateHeaderIndex;

            if (header != null)
            {
                var property = _parser.ParserTemplate.PropertyTemplate;
                if (property == null)
                {
                    _errorService.RegisterError(ParseValidation.PARSER_MISSING_COLUMN, ErrorType.PARSING);
                    return result;
                }
                ValidateFileFormat(header, property);
                CheckDuplicateName(result, reader, header);
            }

            return result;
        }

        private void ValidateFileFormat(IDictionary<string, int> header, IDictionary<string, string> property)
        {
            // Check file format
            if (property.Count == header.Count - 1)
                return;
            else
            {
                var missingColumn = property.FirstOrDefault(x => !header.Keys.Any(p => p == x.Value));
                if (!string.IsNullOrEmpty(missingColumn.Key))
                {
                    _errorService.RegisterError(ParseValidation.PARSER_MISSING_COLUMN, null, new Dictionary<string, object>
                    {
                        { ErrorProperty.ERROR_PROPERTY_NAME, missingColumn.Key }
                    });
                }
            }
        }

        private void CheckDuplicateName(IList<AssetAttribute> attributes, ISheet sheet, IDictionary<string, int> header)
        {
            var columnIndex = header.Keys.ToList().FindIndex(key => key == AssetAttributeHeader.ATTRIBUTE_NAME);
            if (columnIndex < 0)
                return;

            var duplicates = GetDuplicatePositions(sheet, columnIndex);
            foreach (var item in duplicates)
            {
                var existingAttrs = attributes.Where(x => x.AttributeName == item.Key).ToList();

                if (existingAttrs.Count <= 1)
                    continue;

                var attribute = existingAttrs[0];
                var type = attribute.AttributeType;

                for (var attributeIndex = 1; attributeIndex < existingAttrs.Count; attributeIndex++)
                {
                    var duplicateAttribute = existingAttrs[attributeIndex];
                    if (!string.Equals(duplicateAttribute.AttributeType, type, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var rowDuplicate = sheet.GetRow(item.Value[attributeIndex]);
                        var iCell = rowDuplicate.GetCell(columnIndex);
                        _errorService.RegisterError(ParseValidation.PARSER_DUPLICATED_ATTRIBUTE_NAME, iCell, new Dictionary<string, object>
                        {
                            { "PropertyName", ErrorProperty.AssetAttribute.ATTRIBUTE_NAME }
                        });
                        attributes.Remove(duplicateAttribute);
                    }
                    else
                    {
                        attributes.Remove(attribute);
                        attribute = duplicateAttribute;
                    }
                }
            }
        }

        private Dictionary<string, List<int>> GetDuplicatePositions(ISheet sheet, int columnIndex)
        {
            Dictionary<string, List<int>> duplicatePositions = new();
            int rowCount = sheet.LastRowNum + 1;

            for (int row = sheet.FirstRowNum + 1; row < rowCount; row++)
            {
                var cell = sheet.GetRow(row)?.GetCell(columnIndex);
                if (cell != null)
                {
                    string cellValue = cell.ToString().Trim();

                    if (!duplicatePositions.TryGetValue(cellValue, out var positions))
                    {
                        positions = new List<int>();
                        duplicatePositions[cellValue] = positions;
                    }

                    positions.Add(row);
                }
            }

            return duplicatePositions;
        }
    }
}
