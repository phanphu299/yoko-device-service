using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.Validators;
using Device.Consumer.KraftShared.Service.Abstraction;
using ValidationMessage = Device.Consumer.KraftShared.Constant.ErrorMessage.FluentValidation;

namespace Device.Consumer.KraftShared.Repositories
{
    public class MatchRegex : AsyncValidatorBase
    {
        private readonly ISystemContext _systemContext;
        private readonly string _regexKey;
        private readonly string _descriptionKey;
        private readonly bool _acceptNullEmpty;

        public MatchRegex(string regexKey, string descriptionKey, ISystemContext systemContext, bool acceptNullEmpty = false) : base("{Message}")
        {
            _systemContext = systemContext;
            _regexKey = regexKey;
            _descriptionKey = descriptionKey;
            _acceptNullEmpty = acceptNullEmpty;
        }

        protected async override Task<bool> IsValidAsync(PropertyValidatorContext context, CancellationToken cancellation)
        {
            var value = context.PropertyValue.ToString();
            if (_acceptNullEmpty && string.IsNullOrEmpty(value))
                return true;

            if (string.IsNullOrEmpty(value))
            {
                context.MessageFormatter.AppendArgument("Message", ValidationMessage.REQUIRED);
                return false;
            }
            try
            {
                var regexString = await _systemContext.GetValueAsync(_regexKey, null);
                //var szExpression = @"^[\w*\s*]{0,255}$";
                var result = Regex.IsMatch(value, regexString, RegexOptions.IgnoreCase);
                if (!result)
                {
                    var errorMessage = await _systemContext.GetValueAsync(_descriptionKey, null);
                    context.MessageFormatter.AppendArgument("Message", ValidationMessage.GENERAL_INVALID_VALUE);
                }

                return result;
            }
            catch
            {
                // for cant load regex from system
                throw new System.InvalidOperationException("Failed to load validation data.");
            }
        }
    }
}
