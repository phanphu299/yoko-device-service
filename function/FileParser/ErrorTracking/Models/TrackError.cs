using Newtonsoft.Json;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using AHI.Device.Function.Constant;
using System.Collections.Generic;

namespace AHI.Device.Function.FileParser.ErrorTracking.Model
{
    public class TrackError
    {
        [JsonProperty(Order = -2)]
        public string Type { get; set; }
        public string Message { get; set; }
        public IDictionary<string, object> ValidationInfo { get; set; }
        public TrackError(string message, ErrorType errorType = ErrorType.UNDEFINED, IDictionary<string, object> validationInfo = null)
        {
            this.Message = message;
            this.Type = errorType.ToString();
            ValidationInfo = validationInfo;
        }
    }

    public class ExcelTrackError : TrackError
    {
        public string SheetName { get; }
        public string Column { get; }
        public string Row { get; }
        public ExcelTrackError(string message, ICell cell, ErrorType errorType = ErrorType.UNDEFINED, IDictionary<string, object> validationInfo = null)
            : base(message, errorType, validationInfo)
        {
            if (cell != null)
            {
                SheetName = cell.Sheet.SheetName;
                var celref = new CellReference(cell);
                Row = celref.CellRefParts[1];
                Column = celref.CellRefParts[2];
            }
        }
        public ExcelTrackError(string message, string sheetName, int rowIndex, int colIndex, ErrorType errorType = ErrorType.UNDEFINED, IDictionary<string, object> validationInfo = null)
            : base(message, errorType, validationInfo)
        {
            SheetName = sheetName;
            var celref = new CellReference(rowIndex, colIndex);
            if (rowIndex >= 0)
                Row = celref.CellRefParts[1];
            if (colIndex >= 0)
                Column = celref.CellRefParts[2];
        }
    }

    public class JsonTrackError : TrackError
    {
        public JsonTrackError(string message, ErrorType errorType = ErrorType.UNDEFINED, IDictionary<string, object> validationInfo = null)
            : base(message, errorType, validationInfo)
        {
        }
    }

    public class FileIngestionTrackError : TrackError
    {
        public string Key { get; }
        public int Row { get; }
        public FileIngestionTrackError(string message, int rowIndex, string key, ErrorType errorType = ErrorType.UNDEFINED) : base(message, errorType)
        {
            Row = rowIndex;
            Key = key;
        }
    }
}