using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MediatR;
using System;
using Device.Application.Service.Abstraction;

namespace Device.Application.Asset.Command.Handler
{
    class CheckExistingAssetIdsRequestHandler : IRequestHandler<CheckExistingAssetIds, IEnumerable<Guid>>
    {
        private readonly IAssetService _service;

        public CheckExistingAssetIdsRequestHandler(IAssetService service)
        {
            _service = service;
        }

        public Task<IEnumerable<Guid>> Handle(CheckExistingAssetIds request, CancellationToken cancellationToken)
        {
            return _service.CheckExistingAssetIdsAsync(request, cancellationToken);
        }
    }
}
