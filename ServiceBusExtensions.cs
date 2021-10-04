using Microsoft.Azure.WebJobs;

namespace Trustfall.ServiceBus
{
    public static class ServiceBusExtensions
    {
        public static void UseServiceBusSessions(this JobHostConfiguration config, int maxConcurrentSessions = 5)
        {
            var configuration = new ServiceBusSessionConfiguration { MaxConcurrentSessions = maxConcurrentSessions };
            var provider = new ServiceBusSessionTriggerBindingProvider(configuration);
            config.RegisterBindingExtension(provider);
        }
    }
}
