using Microsoft.Extensions.Logging;
using Microsoft.Azure.ServiceBus;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System;

namespace queue_consumer
{
    public class AzureServiceBusConsumer : IConsumer
    {
        private static IQueueClient queueClient;
        private string _connectionString;
        private string _queueName;
        private string _targetServiceName;
        private string _targetServiceAddress;
        private INotifier _notifier;
        private readonly ILogger _logger;
        public AzureServiceBusConsumer(string targetServiceName, string targetServiceAddress, string connectionString, string queueName, INotifier notifier, ILogger logger)
        {
            _targetServiceName = targetServiceName;
            _targetServiceAddress = targetServiceAddress;
            _connectionString = connectionString;
            _queueName = queueName;
            _notifier = notifier;
            _logger = logger;
        }

        public void Consume()
        {
            queueClient = new QueueClient(_connectionString, _queueName, ReceiveMode.PeekLock);
            var sessionHandlerOptions = new SessionHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentSessions = 500,
                AutoComplete = false,
            };
            queueClient.RegisterSessionHandler(ProcessMessagesAsync, sessionHandlerOptions);
        }

        public void StopConsume()
        {
            queueClient.CloseAsync();
        }

        async Task ProcessMessagesAsync(IMessageSession session, Message message, CancellationToken token)
        {
            string traceId = null;
            string spanId = null;
            string uniqueId = null;
            var body = (JObject)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Body));
            if (body.ContainsKey("traceId") && body.ContainsKey("spanId"))
            {
                traceId = body["traceId"].Value<String>();
                spanId = body["spanId"].Value<String>();
                if(body.ContainsKey("outerConversationID"))
                    uniqueId = body["outerConversationID"].Value<String>();
            }
            // Process the message.
            _logger.LogInformation($"Received message: SequenceNumber:{message.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(message.Body)}" + (traceId != null ? "%%" + traceId : "") + (spanId != null ? "%%" + spanId : "") + (uniqueId != null ? "%%" + uniqueId : ""));
            using (HttpClient client = new HttpClient())
            {
                var content = new StringContent(Encoding.UTF8.GetString(message.Body), Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Add("X-B3-TraceId", traceId);
                client.DefaultRequestHeaders.Add("X-B3-SpanId", spanId);
                try
                {
                    HttpResponseMessage response = await client.PostAsync(_targetServiceAddress, content);
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"{response.StatusCode} status code from {_targetServiceName} for {session.SessionId}:" + (responseBody ?? " No info" + (traceId != null ? "%%" + traceId : "") + (spanId != null ? "%%" + spanId : "") + (uniqueId != null ? "%%" + uniqueId : "")));
                    await session.CompleteAsync(message.SystemProperties.LockToken);
                }
                catch (HttpRequestException e)
                {
                    _logger.LogError($"\nException Caught for {session.SessionId}!" + e.Message + (traceId != null ? "%%" + traceId : "") + (spanId != null ? "%%" + spanId : "") + (uniqueId != null ? "%%" + uniqueId : ""));
                    _notifier.SendNotification($"\nException Caught for {session.SessionId}!", "Message:" + e.Message);
                    await session.AbandonAsync(message.SystemProperties.LockToken);
                }
            }
        }

        // Use this handler to examine the exceptions received on the message pump.
        Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            _logger.LogError($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            _logger.LogError("Exception context for troubleshooting:");
            _logger.LogError($"- Endpoint: {context.Endpoint}");
            _logger.LogError($"- Entity Path: {context.EntityPath}");
            _logger.LogError($"- Executing Action: {context.Action}");
            return Task.CompletedTask;
        }

    }
}