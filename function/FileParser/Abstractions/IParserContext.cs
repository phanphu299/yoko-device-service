using Microsoft.Azure.WebJobs;
using AHI.Device.Function.FileParser.BaseExcelParser;
using AHI.Device.Function.FileParser.Constant;
using AHI.Device.Function.FileParser.Template.Abstraction;

namespace AHI.Device.Function.FileParser.Abstraction
{
    public interface IParserContext
    {
        void SetExecutionContext(ExecutionContext context, ParseAction action);
        string GetTemplatePath(string templateName);
        IImportTrackingService GetErrorTracking(string mimeType);
        ExcelDataParser GetParser();
        ExcelTemplate GetTemplate(TemplateType type);
        void SetContextFormat(string key, string format);
        string GetContextFormat(string key);
    }
}