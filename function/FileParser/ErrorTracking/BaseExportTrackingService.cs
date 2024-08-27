using System.Collections.Generic;
using AHI.Device.Function.Constant;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Device.Function.FileParser.ErrorTracking.Model;

namespace AHI.Device.Function.FileParser.ErrorTracking
{
    public abstract class BaseExportTrackingService : IExportTrackingService
    {
        protected ICollection<TrackError> _currentErrors { get; set; }
        public ICollection<TrackError> GetErrors => _currentErrors;

        public bool HasError => (_currentErrors?.Count ?? -1) > 0;

        public virtual void RegisterError(string message, ErrorType errorType = ErrorType.UNDEFINED, IDictionary<string, object> validationInfo = null)
        {
            _currentErrors.Add(new TrackError(message, errorType, validationInfo));
        }
    }
}