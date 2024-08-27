using System.Collections.Generic;
using AHI.Device.Function.Constant;
using AHI.Device.Function.FileParser.BaseExcelParser;
using AHI.Device.Function.FileParser.ErrorTracking.Model;
using AHI.Device.Function.FileParser.Model;

namespace AHI.Device.Function.FileParser.Abstraction
{
    public interface IErrorService
    {
        bool HasError { get; }

        void RegisterError(string message, ErrorType errorType = ErrorType.UNDEFINED, IDictionary<string, object> validationInfo = null);
    }

    public interface IImportTrackingService : IErrorService
    {
        IDictionary<string, ICollection<TrackError>> FileErrors { get; }
        string File { get; set; }
        void Track(TrackModel model, int fileIndex);
        void RegisterError(string message, TrackModel model, string propName, ErrorType errorType = ErrorType.VALIDATING, IDictionary<string, object> validationInfo = null);
        void RegisterError(string message, TrackModel model, string propName, CellIndex cellIndex, ErrorType errorType = ErrorType.VALIDATING, IDictionary<string, object> validationInfo = null);
    }

    public interface IExportTrackingService : IErrorService
    {
        ICollection<TrackError> GetErrors { get; }
    }

    public interface IFileIngestionTrackingService : IErrorService
    {
        ICollection<TrackError> GetErrors { get; }
        void RegisterError(string message, int rowIndex, string key = "", ErrorType errorType = ErrorType.VALIDATING);
    }
}
