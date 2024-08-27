using System;
using System.Collections.Generic;

namespace AHI.Device.Function.Service
{
    public class BooleanDataProcessor : BaseDataProcessor<bool?>
    {
        protected override string DataType => "bool";

        protected override Func<object, IDictionary<string, object>, bool?> ConvertValue => (x, _) =>
        {
            if (bool.TryParse(x.ToString(), out var result))
            {
                return result;
            }
            return null;
        };
    }
}
