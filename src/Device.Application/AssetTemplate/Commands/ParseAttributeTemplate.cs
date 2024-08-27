using System.Collections.Generic;
using Device.Application.Asset.Command;
using Device.Application.AssetTemplate.Command.Model;
using MediatR;

namespace Device.Application.AssetTemplate.Command
{
    public class ParseAttributeTemplate : IRequest<AttributeTemplateParsed>
    {
        public string ObjectType { get; set; }
        public string FileName { get; set; }
        public string TemplateId { get; set; }
        public IEnumerable<ParseAttributeRequest> UnsavedAttributes { get; set; }
    }
}
