using System.Collections.Generic;
using AHI.Device.Function.Constant;
using AHI.Device.Function.FileParser.BaseExcelParser;
using AHI.Device.Function.FileParser.ErrorTracking.Abstraction;
using AHI.Device.Function.FileParser.ErrorTracking.Model;
using AHI.Device.Function.FileParser.Model;

namespace AHI.Device.Function.FileParser.ErrorTracking
{
    public class JsonTrackingService : BaseImportTrackingService, IJsonTrackingService
    {

        public override void RegisterError(string message, TrackModel model, string propName, ErrorType errorType = ErrorType.VALIDATING, IDictionary<string, object> validationInfo = null)
        {
            RegisterError(message, errorType, validationInfo);
        }

        public override void RegisterError(string message, TrackModel model, string propName, CellIndex cellIndex, ErrorType errorType = ErrorType.VALIDATING, IDictionary<string, object> validationInfo = null)
        {
            RegisterError(message, errorType, validationInfo);
        }

        protected override FileTrackInfo InitTrackInfo(TrackModel model, int fileIndex)
        {
            return new JsonTrackInfo
            {
                Index = fileIndex
            };
        }
    }
}