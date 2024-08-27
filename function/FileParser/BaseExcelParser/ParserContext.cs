using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Device.Function.FileParser.Constant;
using AHI.Device.Function.FileParser.Template.Abstraction;

namespace AHI.Device.Function.FileParser.BaseExcelParser
{
    public class ParserContext : IParserContext
    {
        private readonly IEnumerable<ExcelTemplate> _templates;
        private readonly ExcelDataParser _parser;
        private readonly IDictionary<string, IImportTrackingService> _errorHandlers;
        private readonly IDictionary<ParseAction, string> _templateSubDirectoryMapping;
        private string _templateExecutionDirectory;
        private string _templateDirectory;
        private IDictionary<string, string> _formats;

        public ParserContext(IEnumerable<ExcelTemplate> templates,
                             ExcelDataParser parser, IDictionary<string, IImportTrackingService> errorHandlers)
        {
            _templates = templates;
            _parser = parser;
            _errorHandlers = errorHandlers;
            _templateSubDirectoryMapping = new Dictionary<ParseAction, string>
            {
                {ParseAction.IMPORT, "ImportTemplate"},
                {ParseAction.EXPORT, "ExportTemplate"}
            };
            _formats = new Dictionary<string, string>();
        }

        public void SetExecutionContext(ExecutionContext context, ParseAction action)
        {
            _templateExecutionDirectory = context.FunctionAppDirectory;
            _templateDirectory = _templateSubDirectoryMapping[action];
        }

        public string GetTemplatePath(string templateName)
        {
            return System.IO.Path.Combine(_templateExecutionDirectory, "AppData", _templateDirectory, templateName);
        }

        public IImportTrackingService GetErrorTracking(string mimeType) => _errorHandlers[mimeType];
        public ExcelDataParser GetParser() => _parser;

        public ExcelTemplate GetTemplate(TemplateType type)
        {
            foreach (var template in _templates)
            {
                if (template.TemplateType == type)
                {
                    return template;
                }
            }
            throw new NotSupportedException("Cannot find template for this entity");
        }

        public void SetContextFormat(string key, string format)
        {
            _formats[key] = format;
        }

        public string GetContextFormat(string key)
        {
            return _formats.TryGetValue(key, out var result) ? result : null;
        }
    }
}