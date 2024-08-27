using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using Device.Application.Asset.Command.Model;
using Device.Application.Constant;
using Device.Application.Exception;
using static AHI.Infrastructure.Exception.Model.ValidationResultApiResponse;
namespace Device.ApplicationExtension.Extension
{
    public static class SendConfigurationExtension
    {
        public static Task<SendConfigurationResultMutipleDetailDto<T>> HandleSendConfigurationResult<T>(this Task<T> task, T input)
        {
            return task.ContinueWith((t) =>
            {
                if (t.IsCanceled || t.IsFaulted)
                {
                    string message = Status.RESULT_FAIL;
                    IEnumerable<FieldFailureMessage> fields = null;
                    if (t.Exception.InnerException is BaseException exception)
                    {
                        message = exception.DetailCode ?? exception.ErrorCode ?? exception.Message;
                        fields = exception is EntityValidationException validateEx ? validateEx.Failures.GenerateFieldFailureMessage() : null;
                    }
                    return Task.FromResult(new SendConfigurationResultMutipleDetailDto<T>(false, message, fields, input));
                }
                else
                {
                    return Task.FromResult(new SendConfigurationResultMutipleDetailDto<T>(true, Status.RESULT_SUCCESS, null, t.Result));
                }
            }).Unwrap();
        }
    }
}