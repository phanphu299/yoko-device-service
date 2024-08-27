using System.Collections.Generic;

namespace Device.Application.Asset.Command.Model
{
    public class ValidateAssetResponse
    {
        public bool IsSuccess { get; set; }
        public string ErrorCode { get; set; }
        public IDictionary<string, object> ErrorDetails { get; set; }

        public static ValidateAssetResponse Success => new ValidateAssetResponse(isSuccess: true, null, null);

        public ValidateAssetResponse()
        {
        }

        public ValidateAssetResponse(bool isSuccess, string errorCode, IDictionary<string, object> errorDetails)
        {
            IsSuccess = isSuccess;
            ErrorCode = errorCode;
            ErrorDetails = errorDetails;
        }
    }
}
