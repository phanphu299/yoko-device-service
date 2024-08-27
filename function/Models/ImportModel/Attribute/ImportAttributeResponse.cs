using System.Collections.Generic;

namespace AHI.Device.Function.Model.ImportModel.Attribute
{
    public class ImportAttributeResponse
    {
        public IEnumerable<AttributeTemplate> Attributes { get; set; } = new List<AttributeTemplate>();
        public IEnumerable<ErrorDetail> Errors { get; set; }
        public ImportAttributeResponse() { }
        public ImportAttributeResponse(IEnumerable<AttributeTemplate> attributes, IEnumerable<ErrorDetail> errors)
        {
            Attributes = attributes;
            Errors = errors;
        }
    }
    public class ErrorDetail
    {
        public string Detail { get; set; }
        public string Column { get; set; }
        public string Row { get; set; }
        public string Type { get; set; }
        public string ColumnName { get; set; }
        public string ColumnValue { get; set; }
    }
}