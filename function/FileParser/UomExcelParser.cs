using System;
using System.Collections.Generic;
using NPOI.SS.UserModel;
using AHI.Device.Function.Model.ImportModel;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Infrastructure.Import.Handler;

namespace AHI.Device.Function.FileParser
{
    public class UomExcelParser : ExcelFileHandler<Uom>
    {
        private readonly IServiceProvider _provider;
        private readonly IParserContext _context;
        private ISheetParser<Uom> _parser;

        public UomExcelParser(IServiceProvider provider, IParserContext context)
        {
            _provider = provider;
            _context = context;
        }

        protected override IEnumerable<Uom> Parse(ISheet reader)
        {
            if (_parser is null)
                _parser = ISheetParser<Uom>.GetParser(_provider, _context);
            return _parser.ParseSheet(reader);
        }
    }
}
