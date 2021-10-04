using Microsoft.ServiceBus.Messaging;
using System;

namespace Trustfall.ServiceBus
{
    public sealed class ServiceBusSessionTriggerAttribute : Attribute
    {
        public string Connection { get; set; }
        public string QueueName { get; }
        public AccessRights Access { get; }

        public ServiceBusSessionTriggerAttribute(string queueName, AccessRights access = AccessRights.Listen)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentException($"'{nameof(queueName)}' cannot be null or empty", nameof(queueName));
            }

            QueueName = queueName;
            Access = access;
        }
    }
}
