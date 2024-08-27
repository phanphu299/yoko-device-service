using System.Collections.Generic;

namespace Device.Application.Uom.Command.Model
{
    public class ArchiveUomDataDto
    {
        public IEnumerable<ArchiveUomDto> Uoms { get; set; }
    }
}
