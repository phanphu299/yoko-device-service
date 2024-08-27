using System;
using System.Collections.Generic;

namespace Device.Application.Model
{
    internal class BrokerIotDeviceDto : Dictionary<string, object>
    {
        private const string brokerIdFieldName = "brokerId";
        const string projectIdFieldName = "projectId";

        public Guid? BrokerId
        {
            get
            {
                if (this.ContainsKey(brokerIdFieldName) && Guid.TryParse(this[brokerIdFieldName]?.ToString(), out var brokerId))
                {
                    return brokerId;
                }
                return null;
            }

            set => this[brokerIdFieldName] = value?.ToString();
        }

        public string ProjectId
        {
            get
            {
                if (this.ContainsKey(projectIdFieldName))
                {
                    return this[projectIdFieldName]?.ToString();
                }
                return null;
            }
            set => this[projectIdFieldName] = value;
        }

        public string Username
        {
            get
            {
                if (this.ContainsKey("username"))
                {
                    return this["username"]?.ToString();
                }
                return null;
            }
        }

        public string BrokerType
        {
            get
            {
                if (this.ContainsKey("brokerType"))
                {
                    return this["brokerType"]?.ToString();
                }
                return null;
            }
        }
    }
}