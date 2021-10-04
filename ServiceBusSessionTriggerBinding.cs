using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Trustfall.ServiceBus
{
    class ServiceBusSessionTriggerBinding : ITriggerBinding
    {
        readonly ParameterInfo _parameter;
        readonly ServiceBusSessionTriggerAttribute _attribute;
        private readonly ServiceBusSessionConfiguration _configuration;
        readonly BindingDataProvider _bindingDataProvider;

        public ServiceBusSessionTriggerBinding(ParameterInfo parameter, ServiceBusSessionTriggerAttribute attribute, ServiceBusSessionConfiguration configuration)
        {
            _parameter = parameter;
            _attribute = attribute;
            _configuration = configuration;
            _bindingDataProvider = BindingDataProvider.FromType(parameter.ParameterType);
        }

        public Type TriggerValueType => typeof(IMessageSession);

        public IReadOnlyDictionary<string, Type> BindingDataContract => _bindingDataProvider?.Contract;

        public async Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            var session = value as IMessageSession;
            context.Trace.Verbose($"ServiceBusSessionTriggerBinding.BindAsync() for ServiceBus SessionId={session?.SessionId}");

            IValueProvider provider = new MessageSessionValueProvider(session);
            var providerVal = await provider.GetValueAsync();
            var bindingData = _bindingDataProvider?.GetBindingData(providerVal);

            var result = new TriggerData(provider, bindingData);

            return result;
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            IListener listener = new ServiceBusSessionListener(context.Executor, _attribute, _configuration);
            return Task.FromResult(listener);
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor
            {
                Name = _parameter.Name,
                DisplayHints = new ParameterDisplayHints
                {
                    DefaultValue = "MyDefaultvalue",
                    Description = "My Description",
                    Prompt = "My Prompt"
                }
            };
        }
    }
}
