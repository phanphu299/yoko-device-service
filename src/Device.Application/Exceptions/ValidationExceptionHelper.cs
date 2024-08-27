using System.Collections.Generic;

namespace AHI.Infrastructure.Exception.Helper
{
    public static class ValidationExceptionHelper
    {
        public static EntityValidationException GenerateNotFoundValidation(string fieldName, IDictionary<string, object> payload = null)
        {
            return EntityValidationExceptionHelper.GenerateException(
                fieldName,
                ExceptionErrorCode.DetailCode.ERROR_VALIDATION_NOT_FOUND,
                detailCode: ExceptionErrorCode.DetailCode.ERROR_VALIDATION_SOME_ITEMS_DELETED,
                payload: payload);
        }

        public static EntityValidationException GenerateDuplicateValidation(string fieldName)
        {
            return EntityValidationExceptionHelper.GenerateException(
                fieldName,
                ExceptionErrorCode.DetailCode.ERROR_VALIDATION_DUPLICATED);
        }

        public static EntityValidationException GenerateRequiredValidation(string fieldName)
        {
            return EntityValidationExceptionHelper.GenerateException(
                fieldName,
                ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
        }

        public static EntityValidationException GenerateInvalidValidation(string fieldName)
        {
            return EntityValidationExceptionHelper.GenerateException(
                fieldName,
                ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
        }

        public static EntityValidationException GenerateInvalidValidation(string fieldName, string errorCode, IDictionary<string, object> payload = null)
        {
            return EntityValidationExceptionHelper.GenerateException(fieldName, errorCode, payload: payload);
        }
}
}