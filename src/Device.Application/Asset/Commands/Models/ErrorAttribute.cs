using System.Collections.Generic;

namespace Device.Application.Asset.Command.Model
{
    public class ErrorAttribute
    {
        public string Id { get; set; }

        public IList<ErrorField> Fields { get; set; }

        public ErrorAttribute()
        {
            Fields = new List<ErrorField>();
        }

        public ErrorAttribute(string id, IList<ErrorField> fields)
        {
            Id = id;
            Fields = fields;
        }
    }
}
