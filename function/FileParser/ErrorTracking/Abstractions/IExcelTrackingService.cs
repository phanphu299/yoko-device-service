using System.Collections.Generic;
using NPOI.SS.UserModel;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Device.Function.FileParser.Constant;

namespace AHI.Device.Function.FileParser.ErrorTracking.Abstraction
{
    public interface IExcelTrackingService : IImportTrackingService
    {
        void SetTemplateInfo(string sheetName, Placement placement, IDictionary<string, int> template, string sheetNameProperty = null);
        void RegisterError(string message, string sheetName);
        void RegisterError(string message, ICell cell, IDictionary<string, object> validationInfo = null);
    }
}