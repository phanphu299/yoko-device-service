using System;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Asset.Command.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IAssetAssemblyService
    {
        Task<AttributeAssemblyDto> GenerateAssemblyAsync(Guid id, CancellationToken token);
    }
}