using System;
using Device.Application.Enum;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;

namespace Device.Application.Asset.Command
{
    public class ValidateDeviceTemplateExpression : IRequest<bool>
    {
        public Guid DeviceTemplateId { get; set; }
        public JsonPatchDocument Data { set; get; }
        public ValidationType ValidateType { get; set; } = ValidationType.Asset;
    }
}
