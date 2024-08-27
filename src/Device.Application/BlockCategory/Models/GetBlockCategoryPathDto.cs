using System;
namespace Device.Application.BlockCategory.Model
{
    public class GetBlockCategoryPathDto
    {
        public Guid Id { get; set; }

        // 1,2,3
        public string PathId { get; set; }

        // a1,a2,a3
        public string PathName { get; set; }

        public GetBlockCategoryPathDto(Guid id, string pathId, string pathName)
        {
            Id = id;
            PathId = pathId;
            PathName = pathName;
        }
    }
}
