using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace Trustfall.ServiceBus
{
    public class ServiceBusSessionListener : IListener
    {
        readonly ITriggeredFunctionExecutor _triggerExecutor;
        readonly ServiceBusSessionConfiguration _configuration;
        readonly string _connectionString;
        readonly string _queueName;
        readonly SemaphoreSlim semaphore;
        readonly SessionClient sessionClient;

        public ServiceBusSessionListener(ITriggeredFunctionExecutor triggerExecutor, ServiceBusSessionTriggerAttribute attribute, ServiceBusSessionConfiguration configuration)
        {
            _triggerExecutor = triggerExecutor;
            _configuration = configuration;

            _connectionString = ConfigurationManager.ConnectionStrings[attribute.Connection].ConnectionString;

            _queueName = attribute.QueueName;
            if (_queueName.StartsWith("%") && _queueName.EndsWith("%"))
                _queueName = ConfigurationManager.AppSettings[_queueName.Substring(1, _queueName.Length - 2)];

            semaphore = new SemaphoreSlim(_configuration.MaxConcurrentSessions);
            sessionClient = new SessionClient(_connectionString, _queueName);
        }

        public async Task StartAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            while (!ct.IsCancellationRequested)
            {
                await semaphore.WaitAsync(ct);
                if (!ct.IsCancellationRequested)
                {
                    var session = await OpenSessionAsync(ct);

                    if (!ct.IsCancellationRequested)
                    {
                        _ = ProcessOneSessionAndReleaseAsync(session, ct);
                    }
                    else
                    {
                        System.Diagnostics.Trace.TraceInformation($"Releasing semaphore due to cancellation ({Thread.CurrentThread.ManagedThreadId})");
                        semaphore.Release();
                    }
                }
            }
        }

        private async Task<IMessageSession> OpenSessionAsync(CancellationToken ct)
        {
            //Run a parallel wait task that triggers if the global cancellation token is triggered, or a message is received
            var waitForSessionTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Task waitForSessionTask = DelayUntilCancellationToken(waitForSessionTokenSource.Token);

            Task<IMessageSession> sessionReceiveTask;
            do
            {
                //Run a blocking session receive task, because it doesn't accept a cancellationtoken
                sessionReceiveTask = sessionClient.AcceptMessageSessionAsync();

                //Wait until either a session is received, times out OR the global cancellation token is triggered
                await Task.WhenAny(sessionReceiveTask, waitForSessionTask);

            } while (sessionReceiveTask.IsFaulted); //Repeat connection until it no longer is timed out


            //Return the received session, or null if the global cancellation token was triggered
            if (sessionReceiveTask.IsCompleted)
            {
                waitForSessionTokenSource.Cancel();
                return sessionReceiveTask.Result;
            }
            else
            {
                return null;
            }
        }

        private static Task DelayUntilCancellationToken(CancellationToken waitForSessionToken)
        {
            return Task.Run(async () =>
            {
                while (!waitForSessionToken.IsCancellationRequested)
                {
                    await Task.Delay(int.MaxValue, waitForSessionToken);
                }
            });
        }

        private async Task ProcessOneSessionAndReleaseAsync(IMessageSession session, CancellationToken ct)
        {
            try
            {
                var input = new TriggeredFunctionData { TriggerValue = session };

                await _triggerExecutor.TryExecuteAsync(input, ct);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("ServiceBusSessionListener caught unhandled exception during TryExecuteAsync:" + ex.Message);
            }
            finally
            {
                try
                {
                    await session.CloseAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("Failed to close Servicebus session: " + ex.Message);
                }

                semaphore.Release();
            }
        }

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
