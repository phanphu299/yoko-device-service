using System.Collections.Generic;
using System.IO;
using AHI.Device.Function.FileParser.ErrorTracking.Model;
using AHI.Infrastructure.Audit.Model;

namespace AHI.Device.Function.Model.Notification
{
    public class FileIngestionLogDetail : ImportExportDetailPayload<TrackError>
    {
        public string File { get; set; }
        public FileIngestionLogDetail(string filePath, ICollection<TrackError> errors) : base(filePath, errors)
        {
            File = Path.GetFileName(filePath);
        }
    }
}
