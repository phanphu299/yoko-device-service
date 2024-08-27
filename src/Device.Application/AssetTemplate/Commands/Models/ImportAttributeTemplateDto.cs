using System.Collections.Generic;
using Device.Application.AssetAttributeTemplate.Command.Model;

namespace Device.Application.AssetTemplate.Command.Model
{
    public class AttributeTemplateParsed
    {
        public IEnumerable<AttributeParsed> Attributes { get; set; }
        public List<ErrorDetail> Errors { get; set; }
        public AttributeTemplateParsed()
        {
            Attributes = new List<AttributeParsed>();
            Errors = new List<ErrorDetail>();
        }
    }
    public class AttributeTemplateParsedDto
    {
        public IEnumerable<AttributeParsedDto> Attributes { get; set; }
        public List<ErrorDetail> Errors { get; set; }
        public AttributeTemplateParsedDto()
        {
            Attributes = new List<AttributeParsedDto>();
            Errors = new List<ErrorDetail>();
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
