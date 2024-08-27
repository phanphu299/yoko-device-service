using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Device.Consumer.KraftShared.Models.QueryModel
{
    public class DeviceInformationWithTopicNameQueryModel
    {
        public string ProjectId { get; set; }
        public string TopicName { get; set; }
        public string BrokerType { get; set; }
    }
}
