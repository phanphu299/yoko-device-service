using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using AHI.Device.Function.Model.ImportModel;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Infrastructure.Import.Handler;
using AHI.Device.Function.FileParser.Model;
using AHI.Device.Function.FileParser.BaseExcelParser;
using System.Linq;
using Function.Extension;

namespace AHI.Device.Function.FileParser
{
    public class AssetTemplateExcelParser : ExcelFileHandler<AssetTemplate>
    {
        private readonly IServiceProvider _provider;
        private readonly IParserContext _context;
        private ISheetParser<AssetTemplate> _parser;

        public AssetTemplateExcelParser(IServiceProvider provider, IParserContext context)
        {
            _provider = provider;
            _context = context;
        }

        protected override IEnumerable<AssetTemplate> Parse(ISheet reader)
        {
            if (_parser is null)
                _parser = ISheetParser<AssetTemplate>.GetParser(_provider, _context, new CellIndex(1, 0));

            //Compare first cell is Tag
            _parser.SetCustomHeaderNeedToCheck(new List<CustomHeaderRange> { new CustomHeaderRange(0, 0, 0) });

            var assetTemplates = _parser.ParseSheet(reader, false, new CellIndex(1, 0)).ToList();

            var tagValues = reader.GetCell(new CellIndex(0, 1))?.StringCellValue;
            if (!string.IsNullOrEmpty(tagValues))
            {
                AttachTagValues(assetTemplates, tagValues);
            }
            return assetTemplates;
        }

        private void AttachTagValues(IList<AssetTemplate> assetTemplates, string tagValues)
        {
            var assetTemplateCloneList = new List<AssetTemplate>(assetTemplates);

            foreach (var assetTemplate in assetTemplateCloneList)
            {
                assetTemplate.Tags = tagValues;
            }
        }
    }
}