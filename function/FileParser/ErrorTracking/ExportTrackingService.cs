using System.Collections.Generic;
using AHI.Device.Function.FileParser.ErrorTracking.Model;

namespace AHI.Device.Function.FileParser.ErrorTracking
{
    public class ExportTrackingService : BaseExportTrackingService
    {
        public ExportTrackingService()
        {
            _currentErrors = new List<TrackError>();
        }
    }
}