using System.Collections.Generic;
using System.Linq;
using static AHI.Infrastructure.Exception.Model.ValidationResultApiResponse;
namespace Device.Application.Exception
{
    public static class ExceptionExtension
    {
        public static IEnumerable<FieldFailureMessage> GenerateFieldFailureMessage(this IDictionary<string, string[]> failures)
        {
            return failures.SelectMany(fieldFailures => fieldFailures.Value.Select(item => new FieldFailureMessage
            {
                Name = fieldFailures.Key,
                ErrorCode = item,
                ErrorDetail = null
            }));
        }
    }
}