using System.Collections.Generic;
using AHI.Device.Function.Constant;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Device.Function.FileParser.ErrorTracking.Model;

namespace AHI.Device.Function.FileParser.ErrorTracking
{
    public class FileIngestionTrackingService : IFileIngestionTrackingService
    {
        private ICollection<TrackError> _currentErrors { get; set; }
        public FileIngestionTrackingService()
        {
            _currentErrors = new List<TrackError>();
        }
        public ICollection<TrackError> GetErrors => _currentErrors;

        public bool HasError => (_currentErrors?.Count ?? -1) > 0;

        public void RegisterError(string message, int rowIndex, string key = "", ErrorType errorType = ErrorType.VALIDATING)
        {
            _currentErrors.Add(new FileIngestionTrackError(message, rowIndex, key, errorType));
        }

        public void RegisterError(string message, ErrorType errorType = ErrorType.UNDEFINED, IDictionary<string, object> validationInfo = null)
        {
            _currentErrors.Add(new TrackError(message, errorType, validationInfo));
        }
    }
}
