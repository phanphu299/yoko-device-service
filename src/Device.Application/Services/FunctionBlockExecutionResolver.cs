using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.Interceptor.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.Exception;
using System.Collections.Concurrent;

namespace Device.Application.Service
{
    public class FunctionBlockExecutionResolver : IFunctionBlockExecutionResolver
    {
        private readonly IDictionary<string, Assembly> _interceptors;
        private readonly ICompilerService _compilerService;
        public FunctionBlockExecutionResolver(ICompilerService compilerService)
        {
            _interceptors = new ConcurrentDictionary<string, Assembly>();
            _compilerService = compilerService;
        }
        public IFunctionBlockExecutionRuntime ResolveInstance(string content)
        {
            try
            {
                var contentMd5 = content.CalculateMd5Hash();
                if (!_interceptors.ContainsKey(contentMd5))
                {
                    // build the engine code here
                    var template = @$"
                        //Sample code. Please update accordingly;
                        using System;
                        using System.Linq;
                        using System.Threading.Tasks;
                        using System.Collections.Generic;
                        using Device.Application.BlockFunction.Model;
                        using AHI.Infrastructure.SharedKernel.Extension;
                        using Device.Application.Enum;
                        using System.Globalization;
                        namespace Device.Application.Service
                        {{
                            public class BlockFunctionRuntimeWrapper : BaseFunctionBlockExecutionRuntime
                            {{
                                public override async Task ExecuteAsync()
                                {{
                                    {content}
                                }}
                            }}
                        }}";
                    _interceptors[contentMd5] = _compilerService.CompileToAssembly(contentMd5, template);
                }
                // make it transient
                var assembly = _interceptors[contentMd5];
                return CreateIntance(assembly);
            }
            catch (System.Exception e)
            {
                throw new GenericProcessFailedException(e.Message);
            }
        }
        private IFunctionBlockExecutionRuntime CreateIntance(Assembly assembly)
        {
            var type = assembly.GetTypes().FirstOrDefault(t => typeof(IFunctionBlockExecutionRuntime).IsAssignableFrom(t));
            return Activator.CreateInstance(type) as IFunctionBlockExecutionRuntime;
        }
    }
}
