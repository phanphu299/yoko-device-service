using System;
using System.Collections.Generic;

namespace Device.Application.Asset.Command.Model
{
    public class AssetTableDto
    {
        public Guid TableId { get; set; }
        public string TableName { get; set; }
        public Guid AssetId { get; set; }
        public IEnumerable<AssetColumnDto> Columns { get; set; }

        public AssetTableDto()
        {
            Columns = new List<AssetColumnDto>();
        }
    }

    public class AssetColumnDto
    {
        public int ColumnId { get; set; }
        public string ColumnName { get; set; }
        public bool ColumnIsPrimary { get; set; }
        public string ColumnTypeCode { get; set; }
        public string ColumnDefaultValue { get; set; }
        public bool ColumnAllowNull { get; set; }
    }
}