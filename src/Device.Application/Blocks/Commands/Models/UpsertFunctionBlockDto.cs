using System.Collections.Generic;
using Device.Application.SharedKernel;
namespace Device.Application.Block.Command.Model
{
    public class UpsertFunctionBlockDto
    {
        public IEnumerable<BaseJsonPathDocument> Data { set; get; } = new List<BaseJsonPathDocument>();
    }
}
