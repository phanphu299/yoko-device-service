using System;
using System.Collections.Generic;
using Device.Application.BlockFunctionCategory.Model;
using MediatR;

namespace Device.Application.BlockFunctionCategory.Command
{
    public class ArchiveBlockCategory : IRequest<IEnumerable<ArchiveBlockCategoryDto>>
    {
        public DateTime ArchiveTime { get; set; } = DateTime.UtcNow;
    }
}