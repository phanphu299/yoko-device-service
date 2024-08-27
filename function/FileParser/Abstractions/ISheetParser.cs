using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using NPOI.SS.UserModel;
using AHI.Device.Function.FileParser.Constant;
using AHI.Device.Function.FileParser.BaseExcelParser;
using AHI.Device.Function.FileParser.Template.Abstraction;
using AHI.Device.Function.FileParser.ErrorTracking.Abstraction;
using AHI.Device.Function.Constant;
using AHI.Device.Function.FileParser.Model;

namespace AHI.Device.Function.FileParser.Abstraction
{
    public abstract class ISheetParser<T> where T : ImportModel
    {
        protected IExcelTrackingService _errorService { get; private set; }
        protected ExcelDataParser _parser { get; private set; }
        public virtual ExcelTemplate ParserTemplate => null;
        protected IList<CustomHeaderRange> _customHeaderNeedToCheck { get; private set; }
        protected static readonly IDictionary<TemplateType, Type> _parserType = new Dictionary<TemplateType, Type>
        {
            {TemplateType.SIMPLE, typeof(SimpleParser<T>)},
            {TemplateType.COMPLEX, typeof(ComplexParser<T>)}
        };

        public static ISheetParser<T> GetParser(IServiceProvider provider, IParserContext context, CellIndex cellIndex = null)
        {
            var template = ExcelTemplate.GetTemplate(typeof(T).Name, context, cellIndex);

            var sheetParser = provider.GetRequiredService(_parserType[template.TemplateType]) as ISheetParser<T>;
            sheetParser.InitializeParser(template, context);
            return sheetParser;
        }

        private void InitializeParser(ExcelTemplate template, IParserContext context)
        {
            _errorService = context.GetErrorTracking(MimeType.EXCEL) as IExcelTrackingService;
            SetParser(context.GetParser());
            SetTemplate(template);
        }

        protected virtual void SetParser(ExcelDataParser parser)
        {
            _parser = parser;
        }

        protected abstract void SetTemplate(ExcelTemplate template);
        public abstract IEnumerable<T> ParseSheet(ISheet sheet, bool forceGetResult = false, CellIndex cellIndex = null);

        /// <summary>
        /// Compare any row , column from import with template
        /// </summary>
        /// <param name="customHeaderRanges"></param>
        public void SetCustomHeaderNeedToCheck(IList<CustomHeaderRange> customHeaderRanges)
        {
            _customHeaderNeedToCheck = customHeaderRanges;
        }
    }
}
