using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service;
using MediatR;
using Newtonsoft.Json;

namespace Device.Application.Asset.Command.Handler
{
    public class ValidateDeviceTemplateExpressionResquestHandler : IRequestHandler<ValidateDeviceTemplateExpression, bool>
    {
        private readonly RuntimeDeviceTemplateAttributeHandler _service;

        public ValidateDeviceTemplateExpressionResquestHandler(RuntimeDeviceTemplateAttributeHandler service)
        {
            _service = service;
        }

        public virtual async Task<bool> Handle(ValidateDeviceTemplateExpression request, CancellationToken cancellationToken)
        {
            //  var dataTypes = await _dataTypeRepository.AsQueryable().AsNoTracking().ToListAsync();
            foreach (var operation in request.Data.Operations)
            {
                var model = JsonConvert.DeserializeObject<DeviceTemplateExpression>(JsonConvert.SerializeObject(operation.value));
                if (request.ValidateType == Enum.ValidationType.DeviceTemplate)
                {
                    var metrics = model.Metrics.Select(x => (x.DetailId, x.Key, x.DataType));
                    var (result, _) = await _service.ValidateExpressionAsync(request.DeviceTemplateId, model.Expression, model.DataType, metrics);
                    if (!result)
                    {
                        return result;
                    }
                }
            }
            return true;
        }
    }
    internal class DeviceTemplateExpression
    {
        public string Expression { get; set; }
        public string DataType { get; set; }
        public IEnumerable<DeviceTemplateValidationRequest> Metrics { get; set; } = new List<DeviceTemplateValidationRequest>();

    }
    internal class DeviceTemplateValidationRequest
    {
        public Guid DetailId { get; set; }
        public string DataType { get; set; }
        public string Key { get; set; }
    }
}
