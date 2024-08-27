using System;
using System.Collections.Generic;

namespace Device.Application.Service
{
    public class FunctionBlockExecutionInput
    {
        public DiagramFunctionBlockBinding BlockBinding { get; set; }
        public IDictionary<string, object> Payload { get; set; }
    }
    public class DiagramFunctionBlockBinding
    {
        public Guid Id { get; set; }
        public Guid FunctionBlockId { get; set; }

    }
}