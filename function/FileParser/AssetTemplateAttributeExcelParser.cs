using System;
using System.Collections.Generic;
using System.Linq;
using AHI.Device.Function.Constant;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Device.Function.FileParser.ErrorTracking.Abstraction;
using AHI.Device.Function.Model.ImportModel.Attribute;
using AHI.Infrastructure.Import.Handler;
using NPOI.SS.UserModel;
using static AHI.Device.Function.Constant.ErrorMessage;
using Properties = AHI.Device.Function.Constant.ErrorMessage.ErrorProperty.AttributeTemplate;

namespace AHI.Device.Function.FileParser
{
    public class AssetTemplateAttributeExcelParser : ExcelFileHandler<AttributeTemplate>
    {
        private readonly IServiceProvider _provider;
        private readonly IParserContext _context;
        private readonly IExcelTrackingService _errorService;
        private ISheetParser<AttributeTemplate> _parser;

        public AssetTemplateAttributeExcelParser(IServiceProvider provider, IParserContext context, IExcelTrackingService errorService, ISheetParser<AttributeTemplate> parser)
        {
            _provider = provider;
            _context = context;
            _errorService = errorService;
            _parser = parser;
        }

        protected override IEnumerable<AttributeTemplate> Parse(ISheet reader)
        {
            if (_parser is null)
                _parser = ISheetParser<AttributeTemplate>.GetParser(_provider, _context);
            var parser = _parser.ParseSheet(reader, true).ToList();

            var header = _parser.ParserTemplate.TemplateHeaderIndex;
            if (header != null && header.Any())
            {
                var property = _parser.ParserTemplate.PropertyTemplate;
                if(property == null)
                {
                    _errorService.RegisterError(ParseValidation.PARSER_MISSING_COLUMN, ErrorType.PARSING);
                    return parser;
                } 
                ValidateFileFormat(header, property);
                CheckDuplicateName(parser, reader, header);
            }
            return parser;
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

        private void CheckDuplicateName(IList<AttributeTemplate> attributes, ISheet sheet, IDictionary<string, int> header)
        {
            var columnIndex = header.Keys.ToList().FindIndex(key => key == Properties.ATTRIBUTE_NAME.Replace(" ", ""));
            if (columnIndex < 0)
                return;

            var duplicates = GetDuplicatePositions(sheet, columnIndex);
            foreach (var item in duplicates)
            {
                var attrExistings = attributes.Where(x => x.AttributeName == item.Key).ToList();
                            
                if (attrExistings.Count <= 1)
                    continue;

                var attribute = attrExistings[0];
                var type = attribute.AttributeType;

                for (var attributeIndex = 1; attributeIndex < attrExistings.Count; attributeIndex++)
                {
                    var duplicateAttribute = attrExistings[attributeIndex];
                    if (!string.Equals(duplicateAttribute.AttributeType, type, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var rowDuplicate = sheet.GetRow(item.Value[attributeIndex]);
                        var icell = rowDuplicate.GetCell(columnIndex);
                        _errorService.RegisterError(ParseValidation.PARSER_DUPLICATED_ATTRIBUTE_NAME, icell, new Dictionary<string, object>
                        {
                            { ErrorProperty.ERROR_PROPERTY_NAME, ErrorProperty.AssetTemplate.ATTRIBUTE_NAME },
                            { ErrorProperty.ERROR_PROPERTY_VALUE, item.Key }
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
            Dictionary<string, List<int>> valuePositions = new Dictionary<string, List<int>>();
            int rowCount = sheet.LastRowNum + 1;

            for (int row = sheet.FirstRowNum + 1; row < rowCount; row++)
            {
                var cell = sheet.GetRow(row)?.GetCell(columnIndex);
                if (cell != null)
                {
                    string cellValue = cell.ToString().Trim();

                    if (!valuePositions.TryGetValue(cellValue, out var positions))
                    {
                        positions = new List<int>();
                        valuePositions[cellValue] = positions;
                    }

                    positions.Add(row);
                }
            }

            return valuePositions;
        }
    }
}