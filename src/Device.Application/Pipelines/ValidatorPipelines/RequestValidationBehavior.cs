using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using AHI.Infrastructure.Validation.Abstraction;
using AHI.Infrastructure.Exception.Helper;

namespace Device.Application.Pipelines.ValidatorPipelines
{
    public class RequestValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        #region Properties

        private readonly IServiceProvider _serviceProvider;

        #endregion

        #region Constructor

        public RequestValidationBehavior(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     <inheritdoc />
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken,
            RequestHandlerDelegate<TResponse> next)
        {
            // Initialize validation context.
            var context = new ValidationContext<TRequest>(request);

            // Get the instance to be validated.
            var instance = context.InstanceToValidate;
            if (instance == null)
                return await next();

            // Pre-defined validation is available.
            // Do the local validation first.
            var validators = _serviceProvider.GetServices<IValidator<TRequest>>();
            if (validators.Any())
            {
                foreach (var validator in validators)
                {
                    var validationResult = await validator.ValidateAsync(request, cancellationToken);
                    if (validationResult == null)
                        continue;

                    if (!validationResult.IsValid)
                        throw EntityValidationExceptionHelper.GenerateException(validationResult.Errors.ToList());
                }
            }

            // Get the validation failure.
            var dynamicValidator = _serviceProvider.GetService<IDynamicValidator>();
            if (dynamicValidator != null)
                await dynamicValidator.ValidateAsync(instance, cancellationToken);

            return await next();
        }

        #endregion
    }
}
