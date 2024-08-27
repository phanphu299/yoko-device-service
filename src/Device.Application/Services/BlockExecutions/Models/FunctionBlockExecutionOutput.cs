using System;
using System.Collections.Generic;

namespace Device.Application.Service
{
    public class FunctionBlockExecutionOutput
    {
        public Guid Id { get; set; }
        public IDictionary<string, object> Payload { get; set; }
    }
}