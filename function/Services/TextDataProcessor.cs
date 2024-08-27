using System;
using System.Collections.Generic;

namespace AHI.Device.Function.Service
{
    public class TextDataProcessor : BaseDataProcessor<string>
    {
        protected override string DataType => "text";
        protected override Func<object, IDictionary<string, object>, string> ConvertValue => (x, _) => x.ToString();
    }
}
