using Microsoft.Azure.WebJobs.Host.Triggers;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Trustfall.ServiceBus
{
    class ServiceBusSessionTriggerBindingProvider : ITriggerBindingProvider
    {
        private readonly ServiceBusSessionConfiguration configuration;

        public ServiceBusSessionTriggerBindingProvider(ServiceBusSessionConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var binding = CreateTriggerBindingFor(context.Parameter);
            return Task.FromResult(binding);
        }

        private ITriggerBinding CreateTriggerBindingFor(ParameterInfo parameter)
        {
            var attribute = parameter.GetCustomAttribute<ServiceBusSessionTriggerAttribute>(inherit: false);
            if (attribute != null)
                return new ServiceBusSessionTriggerBinding(parameter, attribute, configuration);

            return null;
        }
    }
}
