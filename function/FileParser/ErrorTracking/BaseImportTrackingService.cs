using System;
using System.Collections.Generic;
using AHI.Device.Function.Constant;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Device.Function.FileParser.BaseExcelParser;
using AHI.Device.Function.FileParser.ErrorTracking.Model;
using AHI.Device.Function.FileParser.Model;

namespace AHI.Device.Function.FileParser.ErrorTracking
{
    public abstract class BaseImportTrackingService : IImportTrackingService
    {
        protected ICollection<TrackError> _currentErrors { get; set; }
        protected IDictionary<Type, ModelTrackInfo> _trackInfos { get; set; } = new Dictionary<Type, ModelTrackInfo>();
        public IDictionary<string, ICollection<TrackError>> FileErrors { get; } = new Dictionary<string, ICollection<TrackError>>();

        private string _file;
        public string File
        {
            get => _file;
            set
            {
                _file = value;
                _currentErrors = new List<TrackError>();
                _trackInfos.Clear();
                FileErrors[_file] = _currentErrors;
            }
        }

        public bool HasError => (_currentErrors?.Count ?? -1) > 0;

        public void Track(TrackModel model, int fileIndex)
        {
            if (!_trackInfos.TryGetValue(model.GetType(), out var trackModel))
            {
                trackModel = new ModelTrackInfo();
                _trackInfos[model.GetType()] = trackModel;
            }
            trackModel.TrackObjectInfos[model.TrackId] = InitTrackInfo(model, fileIndex);
        }
        protected abstract FileTrackInfo InitTrackInfo(TrackModel model, int fileIndex);

        public virtual void RegisterError(string message, ErrorType errorType = ErrorType.UNDEFINED, IDictionary<string, object> validationInfo = null)
        {
            _currentErrors.Add(new TrackError(message, errorType, validationInfo));
        }
        public abstract void RegisterError(string message, TrackModel model, string propName, ErrorType errorType = ErrorType.VALIDATING, IDictionary<string, object> validationInfo = null);
        public abstract void RegisterError(string message, TrackModel model, string propName, CellIndex cellIndex, ErrorType errorType = ErrorType.VALIDATING, IDictionary<string, object> validationInfo = null);
    }
}