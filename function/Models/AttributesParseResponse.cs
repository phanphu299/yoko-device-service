using System;
using System.Linq.Expressions;
using AHI.Device.Function.Model.ImportModel.Attribute;

namespace Function.Models
{
    public class AttributesParseResponse
    {
        public string Name { get; set; }

        public string AttributeType { get; set; }

        //deviceTemplate
        //channel
        //markup channel
        //markup device
        //metric

        public string DeviceTemplate { get; set; }
        public string Channel { get; set; }
        public string MarkupChannel { get; set; }
        public string MarkupDevice { get; set; }
        public string Metric { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }
        public bool EnableExpression { get; set; }
        public string Expression { get; set; }
        public string UoM {get;set;}
        public int? DecimalPlace { get; set; }
        public bool? ThousandSeparator { get; set; }
        public string TriggerAttribute {get;set;}
        static Func<AttributeTemplate, AttributesParseResponse> Converter = Projection.Compile();
        private static Expression<Func<AttributeTemplate, AttributesParseResponse>> Projection
        {
            get
            {
                return attribute => new AttributesParseResponse
                {
                    Name = attribute.AttributeName,
                    AttributeType = attribute.AttributeType,
                    DeviceTemplate = attribute.DeviceTemplate,
                    Channel = attribute.Channel,
                    
                };
            }
        }
        public static AttributesParseResponse Create(AttributeTemplate attribute)
        {
            if (attribute != null)
                return Converter(attribute);
            return null;
        }
    }
}
