using System.Collections.Generic;
using NPOI.SS.UserModel;
using AHI.Device.Function.Constant;
using AHI.Device.Function.FileParser.Constant;
using AHI.Device.Function.FileParser.ErrorTracking.Abstraction;
using AHI.Device.Function.FileParser.ErrorTracking.Model;
using AHI.Device.Function.FileParser.Model;
using AHI.Device.Function.FileParser.BaseExcelParser;

namespace AHI.Device.Function.FileParser.ErrorTracking
{
    public class ExcelTrackingService : BaseImportTrackingService, IExcelTrackingService
    {
        private ExcelSheetInfo SheetInfo;
        public void SetTemplateInfo(string sheetName, Placement placement, IDictionary<string, int> template, string sheetNameProperty = null)
        {
            var templateInfo = new Dictionary<string, int>();
            foreach (var item in template)
            {
                templateInfo.Add(item.Key, item.Value);
            }
            if (sheetNameProperty != null)
                templateInfo[sheetNameProperty] = -1;

            SheetInfo = new ExcelSheetInfo(sheetName, placement, template);
        }

        public void RegisterError(string message, string sheetName)
        {
            _currentErrors.Add(new ExcelTrackError(message, sheetName, -1, -1, ErrorType.PARSING));
        }

        public void RegisterError(string message, ICell cell, IDictionary<string, object> validationInfo = null)
        {
            _currentErrors.Add(new ExcelTrackError(message, cell, ErrorType.PARSING, validationInfo));
        }

        public override void RegisterError(string message, TrackModel model, string propName, ErrorType errorType = ErrorType.VALIDATING, IDictionary<string, object> validationInfo = null)
        {
            var modelInfo = _trackInfos[model.GetType()];
            var info = modelInfo.TrackObjectInfos[model.TrackId] as ExcelTrackInfo;

            var sheetName = info.SheetInfo.SheetName;

            var firstIndex = info.Index;
            var secondIndex = -1;
            if (propName != null)
            {
                secondIndex = info.SheetInfo.Template[propName];
                if (secondIndex == -1)
                    firstIndex = -1;
            }

            _currentErrors.Add(info.SheetInfo.Placement switch
            {
                Placement.ROW => new ExcelTrackError(message, sheetName, firstIndex, secondIndex, errorType, validationInfo),
                Placement.COL => new ExcelTrackError(message, sheetName, secondIndex, firstIndex, errorType, validationInfo),
                _ => new TrackError(message, errorType)
            });
        }

        public override void RegisterError(string message, TrackModel model, string propName, CellIndex cellIndex, ErrorType errorType = ErrorType.VALIDATING, IDictionary<string, object> validationInfo = null)
        {
            cellIndex = cellIndex ?? new CellIndex(0, 0);
            var modelInfo = _trackInfos[model.GetType()];
            var info = modelInfo.TrackObjectInfos[model.TrackId] as ExcelTrackInfo;

            var sheetName = info.SheetInfo.SheetName;

            _currentErrors.Add(info.SheetInfo.Placement switch
            {
                Placement.ROW => new ExcelTrackError(message, sheetName, cellIndex.Row, cellIndex.Col, errorType, validationInfo),
                Placement.COL => new ExcelTrackError(message, sheetName, cellIndex.Col, cellIndex.Row, errorType, validationInfo),
                _ => new TrackError(message, errorType)
            });
        }

        protected override FileTrackInfo InitTrackInfo(TrackModel model, int fileIndex)
        {
            return new ExcelTrackInfo
            {
                Index = fileIndex,
                SheetInfo = SheetInfo
            };
        }
    }
}