using Device.Application.AssetTemplate.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;
using System.Threading.Tasks;
using System.Threading;

namespace Device.Application.AssetTemplate.Command.Handler
{
    public class ParseAttributeTemplateHandler : IRequestHandler<ParseAttributeTemplate, AttributeTemplateParsed>
    {
        private readonly IAssetTemplateService _service;
        public ParseAttributeTemplateHandler(IAssetTemplateService service)
        {
            _service = service;
        }

        public Task<AttributeTemplateParsed> Handle(ParseAttributeTemplate request, CancellationToken cancellationToken)
        {
            return _service.ParseAttributeTemplateAsync(request, cancellationToken);
        }
    }
}
