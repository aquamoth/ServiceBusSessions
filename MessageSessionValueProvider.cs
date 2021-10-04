using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs.Host.Bindings;
using System;
using System.Threading.Tasks;

namespace Trustfall.ServiceBus
{
    public class MessageSessionValueProvider : IValueProvider
    {
        private readonly IMessageSession value;

        public Type Type => typeof(IMessageSession);

        public MessageSessionValueProvider(IMessageSession value)
        {
            this.value = value;
        }

        public Task<object> GetValueAsync()
        {
            return Task.FromResult(value as object);
        }

        public string ToInvokeString()
        {
            return "MessageSessionValueProvider.ToInvokeString() result";
        }
    }
}
